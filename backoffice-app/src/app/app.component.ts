import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import * as signalR from '@microsoft/signalr';

interface ChatSession {
  id: string;
  applicant: { name: string; dni: string; phone: string; email: string };
  createdAt: string;
}

interface ChatMessage {
  senderType: string;
  text: string;
  sentAt: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html'
})
export class AppComponent {
  apiUrl = 'http://localhost:5000';
  username = 'asesor1';
  password = '123456';
  token = '';
  advisorId = '';
  isActive = false;

  pendingChats: ChatSession[] = [];
  selectedChatId = '';
  messages: ChatMessage[] = [];
  message = '';

  private hubConnection?: signalR.HubConnection;

  async login() {
    const response = await fetch(`${this.apiUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: this.username, password: this.password })
    });
    const data = await response.json();
    this.token = data.token;
    this.advisorId = data.advisorId;
    await this.connectSignalR();
    await this.loadPendingChats();
  }

  async connectSignalR() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hubs/chat`, { accessTokenFactory: () => this.token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('newIncomingChat', async () => this.loadPendingChats());
    this.hubConnection.on('newMessage', (message: ChatMessage) => {
      this.messages = [...this.messages, message];
    });

    await this.hubConnection.start();
  }

  async loadPendingChats() {
    const response = await fetch(`${this.apiUrl}/api/chats/pending`, {
      headers: { Authorization: `Bearer ${this.token}` }
    });
    this.pendingChats = await response.json();
  }

  async toggleActive() {
    this.isActive = !this.isActive;
    await fetch(`${this.apiUrl}/api/chats/advisor/active`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.token}`
      },
      body: JSON.stringify({ isActive: this.isActive })
    });
  }

  async takeChat(chatId: string) {
    this.selectedChatId = chatId;
    await fetch(`${this.apiUrl}/api/chats/${chatId}/take`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${this.token}` }
    });

    await this.hubConnection?.invoke('JoinChatRoom', `chat-${chatId}`);
    const session = await fetch(`${this.apiUrl}/api/chats/${chatId}`);
    const data = await session.json();
    this.messages = data.messages ?? [];
    await this.loadPendingChats();
  }

  async sendMessage() {
    if (!this.message.trim() || !this.selectedChatId) return;
    await fetch(`${this.apiUrl}/api/chats/${this.selectedChatId}/messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        senderType: 'advisor',
        senderId: this.advisorId,
        text: this.message
      })
    });
    this.message = '';
  }
}

import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import * as signalR from '@microsoft/signalr';

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
  applicant = { name: '', dni: '', phone: '', email: '' };
  message = '';
  messages: ChatMessage[] = [];
  sessionId = '';
  showModal = false;
  statusMessage = '';
  private hubConnection?: signalR.HubConnection;

  async openAdvisorChat() {
    this.showModal = true;
  }

  async createSession() {
    const response = await fetch(`${this.apiUrl}/api/chats/session`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(this.applicant)
    });

    const data = await response.json();
    this.sessionId = data.sessionId;
    this.statusMessage = data.statusMessage;
    this.showModal = false;
    await this.connectSignalR();
  }

  async connectSignalR() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hubs/chat`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('newMessage', (message: ChatMessage) => {
      this.messages = [...this.messages, message];
    });

    await this.hubConnection.start();
    await this.hubConnection.invoke('JoinChatRoom', `chat-${this.sessionId}`);

    const session = await fetch(`${this.apiUrl}/api/chats/${this.sessionId}`);
    const chatData = await session.json();
    this.messages = chatData.messages ?? [];
  }

  async sendMessage() {
    if (!this.message.trim()) return;
    await fetch(`${this.apiUrl}/api/chats/${this.sessionId}/messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        senderType: 'applicant',
        senderId: this.applicant.name,
        text: this.message
      })
    });
    this.message = '';
  }
}

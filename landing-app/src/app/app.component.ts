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
  apiUrl = 'http://localhost:5100';

  // Formulario de postulación (landing)
  currentStep = 1;
  totalSteps = 3;
  postulation = {
    fullName: '',
    dni: '',
    phone: '',
    email: '',
    city: '',
    district: '',
    availability: '',
    experience: ''
  };
  postulationSent = false;

  // Chat flotante
  isChatOpen = false;
  isChatStarted = false;
  isChatClosed = false;
  statusMessage = '';
  sessionId = '';
  applicant = { name: '', dni: '', phone: '', email: '' };
  message = '';
  messages: ChatMessage[] = [];

  private hubConnection?: signalR.HubConnection;

  nextStep() {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  submitPostulation() {
    this.postulationSent = true;
  }

  openChatWidget() {
    this.isChatOpen = true;
  }

  closeChatWidget() {
    this.isChatOpen = false;
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
    this.isChatStarted = true;
    this.isChatClosed = false;
    await this.connectSignalR();
  }

  async connectSignalR() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hubs/chat`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('newMessage', (chatMessage: ChatMessage) => {
      this.messages = [...this.messages, chatMessage];
    });

    this.hubConnection.on('chatClosed', () => {
      this.isChatClosed = true;
      this.statusMessage = 'El chat fue cerrado. Puedes abrir uno nuevo cuando quieras.';
    });

    await this.hubConnection.start();
    await this.hubConnection.invoke('JoinChatRoom', `chat-${this.sessionId}`);

    const session = await fetch(`${this.apiUrl}/api/chats/${this.sessionId}`);
    const chatData = await session.json();
    this.messages = chatData.messages ?? [];
  }

  async closeChat(by: 'postulante' | 'asesor' = 'postulante') {
    if (!this.sessionId || this.isChatClosed) return;

    await fetch(`${this.apiUrl}/api/chats/${this.sessionId}/close`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        closedBy: by === 'asesor' ? 'asesor' : 'postulante',
        reason: 'Cierre manual del chat'
      })
    });

    this.isChatClosed = true;
    this.statusMessage = 'Chat cerrado correctamente.';
  }

  async sendMessage() {
    if (!this.message.trim() || !this.sessionId || this.isChatClosed) return;
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

import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import * as signalR from '@microsoft/signalr';

type ChatStatus = 'Pending' | 'Assigned' | 'Closed';

interface ChatSession {
  id: string;
  applicant: { name: string; dni: string; phone: string; email: string };
  createdAt: string;
  status: ChatStatus;
  assignedAdvisorId?: string;
}

interface ChatMessage {
  senderType: string;
  text: string;
  sentAt: string;
}

interface AdvisorState {
  advisorId: string;
  name: string;
  isActive: boolean;
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
  advisorName = '';
  isActive = false;
  loginError = '';

  chats: ChatSession[] = [];
  advisors: AdvisorState[] = [];
  selectedChatId = '';
  messages: ChatMessage[] = [];
  message = '';
  transferAdvisorId = '';
  transferReason = '';

  notifications: string[] = [];
  isCounterpartTyping = false;

  private typingTimeout?: ReturnType<typeof setTimeout>;
  private stopTypingTimeout?: ReturnType<typeof setTimeout>;
  private hubConnection?: signalR.HubConnection;

  get selectedChat(): ChatSession | undefined {
    return this.chats.find((chat) => chat.id === this.selectedChatId);
  }

  get canReply(): boolean {
    return !!this.selectedChatId && this.selectedChat?.status === 'Assigned' && this.selectedChat?.assignedAdvisorId === this.advisorId;
  }

  get activeTransferAdvisors(): AdvisorState[] {
    return this.advisors.filter((advisor) => advisor.isActive && advisor.advisorId !== this.advisorId);
  }

  get canTakeSelectedChat(): boolean {
    return !!this.selectedChatId && this.selectedChat?.status === 'Pending';
  }

  advisorDisplayName(advisorId?: string): string {
    if (!advisorId) {
      return '-';
    }

    const advisor = this.advisors.find((item) => item.advisorId === advisorId);
    return advisor ? `${advisor.name} (${advisor.advisorId})` : advisorId;
  }

  async login() {
    this.loginError = '';

    const response = await fetch(`${this.apiUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: this.username, password: this.password })
    });

    if (!response.ok) {
      this.loginError = 'No se pudo iniciar sesión. Verifica usuario y contraseña.';
      return;
    }

    const data = await response.json();
    this.token = data.token;
    this.advisorId = data.advisorId;
    this.advisorName = data.advisorName;
    await this.connectSignalR();
    await this.loadChats();
    await this.loadAdvisors();
  }

  async connectSignalR() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hubs/chat`, { accessTokenFactory: () => this.token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('newIncomingChat', async () => this.loadChats());
    this.hubConnection.on('chatUpdated', async () => this.loadChats());
    this.hubConnection.on('advisorStatusChanged', async () => this.loadAdvisors());

    this.hubConnection.on('assignmentNotification', (payload: { sessionId: string; message: string }) => {
      this.notify(payload.message);
      this.loadChats();
    });

    this.hubConnection.on('newMessage', (chatMessage: ChatMessage) => {
      if (this.selectedChatId) {
        this.messages = [...this.messages, chatMessage];
      }
    });

    this.hubConnection.on('typingChanged', (payload: { sessionId: string; senderType: string; isTyping: boolean }) => {
      if (payload.sessionId !== this.selectedChatId || payload.senderType === 'advisor') {
        return;
      }

      this.isCounterpartTyping = payload.isTyping;
      if (payload.isTyping) {
        if (this.stopTypingTimeout) {
          clearTimeout(this.stopTypingTimeout);
        }
        this.stopTypingTimeout = setTimeout(() => {
          this.isCounterpartTyping = false;
        }, 3000);
      }
    });

    this.hubConnection.on('chatClosed', async () => {
      if (this.selectedChatId) {
        await this.loadSelectedChat();
        await this.loadChats();
      }
    });

    await this.hubConnection.start();
  }

  async loadChats() {
    const response = await fetch(`${this.apiUrl}/api/chats`, {
      headers: { Authorization: `Bearer ${this.token}` }
    });

    this.chats = await response.json();
  }

  async loadAdvisors() {
    const response = await fetch(`${this.apiUrl}/api/chats/advisors`, {
      headers: { Authorization: `Bearer ${this.token}` }
    });

    this.advisors = await response.json();
  }

  async loadSelectedChat() {
    if (!this.selectedChatId) {
      return;
    }

    const session = await fetch(`${this.apiUrl}/api/chats/${this.selectedChatId}`);
    const data = await session.json();
    this.messages = data.messages ?? [];
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

  async openChat(chatId: string) {
    if (this.selectedChatId && this.selectedChatId !== chatId) {
      await this.hubConnection?.invoke('LeaveChatRoom', `chat-${this.selectedChatId}`);
    }

    this.selectedChatId = chatId;
    this.transferAdvisorId = '';
    this.transferReason = '';
    this.isCounterpartTyping = false;
    await this.hubConnection?.invoke('JoinChatRoom', `chat-${chatId}`);

    await this.loadSelectedChat();
  }

  async takeChat(chatId: string) {
    const response = await fetch(`${this.apiUrl}/api/chats/${chatId}/take`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${this.token}` }
    });

    if (!response.ok) {
      this.notify('Este chat ya fue tomado por otro asesor.');
      await this.loadChats();
      return;
    }

    this.notify('Tomaste el chat correctamente.');
    await this.loadSelectedChat();
    await this.loadChats();
  }

  async transferChat() {
    if (!this.selectedChatId || !this.transferAdvisorId) {
      return;
    }

    const response = await fetch(`${this.apiUrl}/api/chats/${this.selectedChatId}/transfer`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.token}`
      },
      body: JSON.stringify({
        targetAdvisorId: this.transferAdvisorId,
        reason: this.transferReason || null
      })
    });

    if (!response.ok) {
      this.notify('No se pudo derivar la solicitud. Verifica que el chat aún te pertenezca y el destino esté activo.');
      await this.loadChats();
      await this.loadAdvisors();
      return;
    }

    this.notify('Solicitud derivada correctamente.');
    await this.loadSelectedChat();
    await this.loadChats();
  }

  async closeChat() {
    if (!this.selectedChatId) return;
    await fetch(`${this.apiUrl}/api/chats/${this.selectedChatId}/close`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        closedBy: 'asesor',
        reason: 'Cierre desde backoffice'
      })
    });

    await this.loadSelectedChat();
    await this.loadChats();
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
    await this.sendTyping(false);
  }

  onMessageInput() {
    this.sendTyping(true);

    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }

    this.typingTimeout = setTimeout(() => {
      this.sendTyping(false);
    }, 1200);
  }

  async sendTyping(isTyping: boolean) {
    if (!this.selectedChatId || !this.hubConnection) {
      return;
    }

    await this.hubConnection.invoke('SendTyping', this.selectedChatId, 'advisor', isTyping);
  }

  statusLabel(status: ChatStatus) {
    if (status === 'Pending') return 'Pendiente';
    if (status === 'Assigned') return 'Asignado';
    return 'Cerrado';
  }

  private notify(message: string) {
    this.notifications = [message, ...this.notifications].slice(0, 4);
  }
}

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { NotificationResponse } from "@/features/calendar/types/calendar.types";
import { API_BASE_URL } from "@/lib/constants";

const NOTIFICATION_HUB_PATH = "/hubs/notifications";
const NOTIFICATION_EVENT = "notification";
const RETRY_DELAY_MS = 3_000;

interface NotificationHubOptions {
  accessTokenFactory: () => string;
  onNotification: (notification: NotificationResponse) => void;
  onConnectionStateChange: (isConnected: boolean) => void;
  onReconnected: () => void;
  onConnectionError: (error: unknown) => void;
}

export class NotificationHubService {
  private connection: HubConnection | null = null;
  private retryTimeout: ReturnType<typeof setTimeout> | null = null;

  async connect(options: NotificationHubOptions): Promise<void> {
    await this.disconnect();

    const connection = new HubConnectionBuilder()
      .withUrl(new URL(NOTIFICATION_HUB_PATH, API_BASE_URL).toString(), {
        accessTokenFactory: options.accessTokenFactory,
        // Authentication uses the Bearer token above; no cross-origin cookies are needed.
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on(NOTIFICATION_EVENT, options.onNotification);
    connection.onreconnecting(() => options.onConnectionStateChange(false));
    connection.onreconnected(() => {
      options.onConnectionStateChange(true);
      options.onReconnected();
    });
    connection.onclose(() => {
      options.onConnectionStateChange(false);
      this.scheduleReconnect(connection, options);
    });

    this.connection = connection;
    await this.start(connection, options);
  }

  async disconnect(): Promise<void> {
    if (this.retryTimeout) {
      clearTimeout(this.retryTimeout);
      this.retryTimeout = null;
    }

    const connection = this.connection;
    this.connection = null;

    if (connection) {
      await connection.stop();
    }
  }

  private async start(connection: HubConnection, options: NotificationHubOptions): Promise<void> {
    if (this.connection !== connection || connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    try {
      await connection.start();
      options.onConnectionStateChange(true);
    } catch (error) {
      options.onConnectionStateChange(false);
      options.onConnectionError(error);
      this.scheduleReconnect(connection, options);
    }
  }

  private scheduleReconnect(connection: HubConnection, options: NotificationHubOptions): void {
    if (this.connection !== connection || this.retryTimeout) {
      return;
    }

    this.retryTimeout = setTimeout(() => {
      this.retryTimeout = null;
      void this.start(connection, options);
    }, RETRY_DELAY_MS);
  }
}

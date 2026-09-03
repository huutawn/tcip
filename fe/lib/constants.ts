export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export const STORAGE_KEYS = {
  ACCESS_TOKEN: "tcip_access_token",
  REFRESH_TOKEN: "tcip_refresh_token",
  USER: "tcip_user_data",
  THEME: "tcip_theme",
  LANG: "tcip_lang",
};

export const ROUTES = {
  HOME: "/",
  LOGIN: "/login",
  REGISTER: "/register",
  PROJECTS: "/projects",
  ACTIVITIES: "/activities",
  HR: "/hr",
  MY_TASKS: "/my-tasks",
  CALENDAR: "/",
};

export const DEFAULT_AVATARS = [
  "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=100&auto=format&fit=crop&q=80",
];

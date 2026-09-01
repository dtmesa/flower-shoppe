import { apiClient } from "../../lib/apiClient";

export interface LoginResponse {
  token: string;
  username: string;
}

export interface AdminProfile {
  username: string;
}

export interface UpdateCredentialsInput {
  currentPassword: string;
  newUsername: string;
  newPassword?: string;
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>("/api/auth/login", { username, password });
  return data;
}

export async function getProfile(): Promise<AdminProfile> {
  const { data } = await apiClient.get<AdminProfile>("/api/auth/me");
  return data;
}

export async function updateCredentials(input: UpdateCredentialsInput): Promise<LoginResponse> {
  const { data } = await apiClient.put<LoginResponse>("/api/auth/admin", input);
  return data;
}

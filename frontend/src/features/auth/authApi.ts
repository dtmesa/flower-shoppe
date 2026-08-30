import { apiClient } from "../../lib/apiClient";

export interface LoginResponse {
  token: string;
  username: string;
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>("/api/auth/login", { username, password });
  return data;
}

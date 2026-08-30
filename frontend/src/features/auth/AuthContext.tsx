import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getStoredToken, onUnauthorized, setStoredToken } from "../../lib/apiClient";
import { login as loginRequest } from "./authApi";

interface AuthContextValue {
  isAuthenticated: boolean;
  username: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getStoredToken());
  const [username, setUsername] = useState<string | null>(null);

  useEffect(() => {
    onUnauthorized(() => {
      setToken(null);
      setUsername(null);
    });
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: token !== null,
      username,
      login: async (usernameInput: string, password: string) => {
        const response = await loginRequest(usernameInput, password);
        setStoredToken(response.token);
        setToken(response.token);
        setUsername(response.username);
      },
      logout: () => {
        setStoredToken(null);
        setToken(null);
        setUsername(null);
      },
    }),
    [token, username],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}

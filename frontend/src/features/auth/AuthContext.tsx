import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getStoredToken, onUnauthorized, setStoredToken } from "../../lib/apiClient";
import { getProfile, login as loginRequest } from "./authApi";

interface AuthContextValue {
  isAuthenticated: boolean;
  username: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  // Used after changing credentials in-app: swaps in the freshly-issued token/username without
  // requiring the admin to log in again.
  applySession: (token: string, username: string) => void;
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

  // A stored token survives a page refresh, but the username (kept only in memory) doesn't - so
  // whenever we have a token with no username yet, fetch it once.
  useEffect(() => {
    if (token && !username) {
      getProfile()
        .then((profile) => setUsername(profile.username))
        .catch(() => {});
    }
  }, [token, username]);

  function applySession(nextToken: string, nextUsername: string) {
    setStoredToken(nextToken);
    setToken(nextToken);
    setUsername(nextUsername);
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: token !== null,
      username,
      login: async (usernameInput: string, password: string) => {
        const response = await loginRequest(usernameInput, password);
        applySession(response.token, response.username);
      },
      logout: () => {
        setStoredToken(null);
        setToken(null);
        setUsername(null);
      },
      applySession,
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

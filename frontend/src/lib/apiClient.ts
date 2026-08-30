import axios from "axios";

const TOKEN_STORAGE_KEY = "plumeria_admin_token";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
});

export function getStoredToken(): string | null {
  return sessionStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setStoredToken(token: string | null) {
  if (token) {
    sessionStorage.setItem(TOKEN_STORAGE_KEY, token);
  } else {
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  }
}

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

type UnauthorizedListener = () => void;
let unauthorizedListener: UnauthorizedListener | null = null;

export function onUnauthorized(listener: UnauthorizedListener) {
  unauthorizedListener = listener;
}

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 || error.response?.status === 403) {
      setStoredToken(null);
      unauthorizedListener?.();
    }
    return Promise.reject(error);
  },
);

/** Shape of an ASP.NET Core RFC 7807 (ValidationProblemDetails included) error response. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const problem = error.response?.data as ProblemDetails | undefined;
    const firstValidationMessage = problem?.errors && Object.values(problem.errors)[0]?.[0];
    if (firstValidationMessage) return firstValidationMessage;
    if (problem?.detail) return problem.detail;
    if (problem?.title) return problem.title;
    if (error.message) return error.message;
  }
  return "Something went wrong. Please try again.";
}

/** Shared SWR fetcher: GETs a path off the api client and returns the parsed body. */
export const fetcher = (url: string) => apiClient.get(url).then((response) => response.data);

import { useRef, useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { Eye, EyeOff } from "lucide-react";
import { useAuth } from "./AuthContext";
import { FormError, useDismissingError } from "../../components/FormError";
import { extractErrorMessage } from "../../lib/apiClient";
import { shakeFields } from "../../lib/shake";

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useDismissingError();
  const usernameInputRef = useRef<HTMLInputElement>(null);
  const passwordInputRef = useRef<HTMLInputElement>(null);

  if (isAuthenticated) {
    const redirectTo = (location.state as { from?: string } | null)?.from ?? "/admin";
    return <Navigate to={redirectTo} replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!username.trim() || !password) {
      setError("Enter your username and password.");
      shakeFields(
        username.trim() ? null : usernameInputRef.current,
        password ? null : passwordInputRef.current,
      );
      return;
    }

    setSubmitting(true);
    try {
      await login(username, password);
      navigate("/admin", { replace: true });
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="page page--narrow">
      <h1>Admin Login</h1>
      <form className="form" onSubmit={handleSubmit} noValidate>
        <label>
          Username
          <input
            type="text"
            ref={usernameInputRef}
            autoFocus
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          />
        </label>
        <label>
          Password
          <div className="password-field">
            <input
              type={showPassword ? "text" : "password"}
              ref={passwordInputRef}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
            <button
              type="button"
              className="password-toggle"
              onClick={() => setShowPassword((prev) => !prev)}
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              <Eye size={18} className={`password-toggle-icon${showPassword ? "" : " password-toggle-icon--active"}`} aria-hidden="true" />
              <EyeOff size={18} className={`password-toggle-icon${showPassword ? " password-toggle-icon--active" : ""}`} aria-hidden="true" />
            </button>
          </div>
        </label>
        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? "Logging in..." : "Login"}
        </button>
        <FormError message={error} reserveSpace tight />
      </form>
    </div>
  );
}

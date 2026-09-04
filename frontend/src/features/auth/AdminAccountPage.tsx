import { useEffect, useState, type FormEvent } from "react";
import { Eye, EyeOff } from "lucide-react";
import { useAuth } from "./AuthContext";
import { updateCredentials } from "./authApi";
import { FormError, useDismissingError } from "../../components/FormError";
import { extractErrorMessage } from "../../lib/apiClient";

interface PasswordFieldProps {
  value: string;
  onChange: (value: string) => void;
  invalid?: boolean;
  placeholder?: string;
}

function PasswordField({ value, onChange, invalid, placeholder }: PasswordFieldProps) {
  const [show, setShow] = useState(false);
  return (
    <div className="password-field">
      <input
        type={show ? "text" : "password"}
        className={invalid ? "field-invalid" : undefined}
        placeholder={placeholder}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
      <button
        type="button"
        className="password-toggle"
        onClick={() => setShow((prev) => !prev)}
        aria-label={show ? "Hide password" : "Show password"}
      >
        <Eye size={18} className={`password-toggle-icon${show ? "" : " password-toggle-icon--active"}`} aria-hidden="true" />
        <EyeOff size={18} className={`password-toggle-icon${show ? " password-toggle-icon--active" : ""}`} aria-hidden="true" />
      </button>
    </div>
  );
}

export function AdminAccountPage() {
  const { username, applySession } = useAuth();
  const [newUsername, setNewUsername] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useDismissingError();
  const [invalidFields, setInvalidFields] = useState<Set<string>>(new Set());
  const [success, setSuccess] = useState(false);

  // The field highlights exist only to point at the current error, so they clear alongside it
  // when the shared hook's auto-dismiss timer fires.
  useEffect(() => {
    if (!error) setInvalidFields(new Set());
  }, [error]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setInvalidFields(new Set());
    setSuccess(false);

    if (!currentPassword) {
      setError("Current password is required.");
      setInvalidFields(new Set(["currentPassword"]));
      return;
    }

    if (newPassword && newPassword !== confirmPassword) {
      setError("New password and confirmation do not match.");
      setInvalidFields(new Set(["newPassword", "confirmPassword"]));
      return;
    }

    const usernameToSend = newUsername.trim() || username;
    if (!usernameToSend) {
      setError("Could not determine the current username - try reloading the page.");
      return;
    }

    setSaving(true);
    try {
      const response = await updateCredentials({
        currentPassword,
        newUsername: usernameToSend,
        newPassword: newPassword || undefined,
      });
      applySession(response.token, response.username);
      setNewUsername("");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setSuccess(true);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="admin-account-page">
      <p className="state-message state-message--intro">Update the username and password used to log in to this dashboard.</p>
      {success && <p className="state-message">Credentials updated.</p>}
      <form className="form" onSubmit={handleSubmit} noValidate>
        <label>
          Username
          <input
            type="text"
            placeholder="Leave blank to keep your current username"
            value={newUsername}
            onChange={(e) => setNewUsername(e.target.value)}
          />
        </label>
        <label>
          Current Password
          <PasswordField
            value={currentPassword}
            onChange={setCurrentPassword}
            invalid={invalidFields.has("currentPassword")}
          />
        </label>
        <label>
          New Password
          <PasswordField
            value={newPassword}
            onChange={setNewPassword}
            invalid={invalidFields.has("newPassword")}
            placeholder="Leave blank to keep your current password"
          />
        </label>
        <label>
          Confirm New Password
          <PasswordField
            value={confirmPassword}
            onChange={setConfirmPassword}
            invalid={invalidFields.has("confirmPassword")}
          />
        </label>
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? "Saving..." : "Update"}
        </button>
      </form>
      <FormError message={error} reserveSpace />
    </div>
  );
}

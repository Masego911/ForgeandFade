import React from 'react'; // Provides React for this project's JSX transform.
import { useEffect, useState } from 'react'; // Provides state and a lifecycle hook for the two forms.
import { Link } from 'react-router-dom'; // Uses the application's existing page navigation.
import './PasswordRecovery.css'; // Keeps recovery-page styling in a separate file.

async function postAccount(path, body) { // Sends a JSON request to an account endpoint.
    const response = await fetch(`/api/account/${path}`, { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }); // Sends the form data to the API.
    const data = await response.json().catch(() => ({})); // Reads an API message when one is available.
    if (!response.ok) throw new Error(data.message || 'The request could not be completed. Please try again.'); // Turns an unsuccessful response into a useful form error.
    return data; // Gives the successful response to the form.
} // Ends the API helper.

export function ForgotPasswordPage() { // Displays the form that requests a reset email.
    const [email, setEmail] = useState(''); // Holds the address entered by the customer.
    const [message, setMessage] = useState(''); // Holds the response shown after submission.
    const [error, setError] = useState(''); // Holds a delivery or network error.
    const [busy, setBusy] = useState(false); // Prevents repeated submissions while waiting.
    async function submit(event) { // Handles the form submission without reloading the page.
        event.preventDefault(); // Keeps React in control of the form.
        setError(''); // Clears an earlier error before retrying.
        setBusy(true); // Disables the submit button while the request runs.
        try { const result = await postAccount('forgot-password', { email: email.trim() }); setMessage(result.message); } // Requests a reset link and shows the neutral response.
        catch (failure) { setError(failure.message); } // Shows a configuration or delivery problem.
        finally { setBusy(false); } // Enables the form again after the request finishes.
    } // Ends the submit handler.
    return ( // Starts the recovery page layout.
        <section className="section recovery-page"> {/* Provides space below the navigation on every screen size. */}
            <div className="recovery-panel"> {/* Keeps the form at a readable width. */}
                <span className="eyebrow">ACCOUNT RECOVERY</span> {/* Identifies the purpose of this page. */}
                <h1>Forgot your password?</h1> {/* Gives the form a clear heading. */}
                <p>Enter the email address you used when creating your account. If an account exists, we will email you a link that lasts 30 minutes.</p> {/* Explains what happens without revealing whether an address is registered. */}
                <form onSubmit={submit}> {/* Submits the email to the account API. */}
                    <label htmlFor="recovery-email">Email address</label> {/* Gives the input an accessible name. */}
                    <input id="recovery-email" type="email" autoComplete="email" value={email} onChange={event => setEmail(event.target.value)} required maxLength={254} /> {/* Collects and validates the address in the browser. */}
                    {error && <p className="recovery-error" role="alert">{error}</p>} {/* Announces a failed request. */}
                    {message && <p className="recovery-success" role="status">{message}</p>} {/* Announces the neutral success message. */}
                    <button className="button button-dark" type="submit" disabled={busy}>{busy ? 'PLEASE WAIT…' : 'SEND RESET LINK'}</button> {/* Sends the request once. */}
                </form> {/* Ends the email form. */}
                <p className="recovery-return"><Link to="/login">Back to log in</Link></p> {/* Returns to the login page. */}
            </div> {/* Ends the readable panel. */}
        </section> // Ends the recovery page layout.
    ); // Returns the page to React.
} // Ends the request page.

export function ResetPasswordPage() { // Displays the form opened from a reset email.
    const [token, setToken] = useState(() => new URLSearchParams(window.location.hash.slice(1)).get('token') || ''); // Reads the secret from the URL fragment.
    const [password, setPassword] = useState(''); // Holds the new password until submission.
    const [confirm, setConfirm] = useState(''); // Holds the second password entry.
    const [visible, setVisible] = useState(false); // Controls whether the password is visible.
    const [error, setError] = useState(''); // Holds an invalid-link or API error.
    const [success, setSuccess] = useState(false); // Records a completed reset.
    const [busy, setBusy] = useState(false); // Prevents duplicate submissions.
    useEffect(() => { window.history.replaceState(null, '', window.location.pathname + window.location.search); }, []); // Removes the token from the browser address bar after reading it.
    async function submit(event) { // Handles a request to save the new password.
        event.preventDefault(); // Prevents a full page reload.
        setError(''); // Clears an earlier form error.
        if (password.length < 15 || password.length > 128) { setError('Use a password between 15 and 128 characters.'); return; } // Matches the current backend password length rule.
        if (password !== confirm) { setError('The passwords do not match.'); return; } // Requires both entries to agree.
        setBusy(true); // Disables the submit button during the API request.
        try { await postAccount('reset-password', { token, newPassword: password }); setSuccess(true); setToken(''); setPassword(''); setConfirm(''); } // Uses the one-time token and clears sensitive form state.
        catch (failure) { setError(failure.message); } // Shows an expired link or other API error.
        finally { setBusy(false); } // Enables the form again if needed.
    } // Ends the submit handler.
    return ( // Starts the new-password page layout.
        <section className="section recovery-page"> {/* Provides consistent page spacing. */}
            <div className="recovery-panel"> {/* Keeps the form readable on mobile and desktop. */}
                <span className="eyebrow">ACCOUNT RECOVERY</span> {/* Identifies the page. */}
                <h1>Choose a new password.</h1> {/* Explains the required action. */}
                {success && <p className="recovery-success" role="status">Your password has been reset. You can now log in.</p>} {/* Confirms a completed reset. */}
                {!token && !success && <p className="recovery-error" role="alert">This reset link is missing its token. Request a new link.</p>} {/* Explains a link that cannot be used. */}
                {token && !success && <form onSubmit={submit}> {/* Shows the form only while a token is available. */}
                    <p>Use a unique password or passphrase of 15 to 128 characters.</p> {/* States the current password rule. */}
                    <label htmlFor="new-password">New password</label> {/* Names the first password field. */}
                    <input id="new-password" type={visible ? 'text' : 'password'} autoComplete="new-password" value={password} onChange={event => setPassword(event.target.value)} minLength={15} maxLength={128} required /> {/* Collects the new password. */}
                    <label htmlFor="confirm-password">Confirm new password</label> {/* Names the confirmation field. */}
                    <input id="confirm-password" type={visible ? 'text' : 'password'} autoComplete="new-password" value={confirm} onChange={event => setConfirm(event.target.value)} required /> {/* Checks that the customer typed the intended password. */}
                    <label className="recovery-visibility"><input type="checkbox" checked={visible} onChange={event => setVisible(event.target.checked)} /> Show passwords</label> {/* Lets the customer inspect both entries. */}
                    {error && <p className="recovery-error" role="alert">{error}</p>} {/* Announces a validation or API error. */}
                    <button className="button button-dark" type="submit" disabled={busy}>{busy ? 'SAVING…' : 'RESET PASSWORD'}</button> {/* Submits the token and new password. */}
                </form>} {/* Ends the conditional reset form. */}
                <p className="recovery-return"><Link to={success ? '/login' : '/forgot-password'}>{success ? 'Log in' : 'Request a new link'}</Link></p> {/* Provides the appropriate next step. */}
            </div> {/* Ends the panel. */}
        </section> // Ends the page layout.
    ); // Returns the page to React.
} // Ends the reset page.

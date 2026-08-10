export default function ClientAdminVerificationBridge() {
  if (typeof window !== 'undefined') {
    window.location.href = 'http://localhost:3001/verification';
  }

  return <div className="mx-auto max-w-xl px-4 py-16 text-center text-sm text-slate-500">Redirecting to the admin verification console...</div>;
}

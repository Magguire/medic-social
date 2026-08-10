import React from 'react';
import Link from 'next/link';
import { useAuth } from '../lib/useAuth';
import { useRouter } from 'next/router';

export const AdminLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, logout } = useAuth();
  const router = useRouter();

  return (
    <div className="flex min-h-screen bg-gray-100">
      {/* sidebar */}
      <aside className="w-64 bg-white border-r border-gray-200">
        <div className="p-6">
          <Link href="/" className="text-2xl font-bold text-blue-600">
            medicSocial
          </Link>
        </div>
        <nav className="px-4">
          <ul className="space-y-2">
            <li>
              <Link href="/admin/verification" className="block py-2 px-3 rounded hover:bg-gray-100">
                Dashboard
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Job Posts
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Professionals
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Employers
              </Link>
            </li>
            <li>
              <Link href="/admin/verification" className="block py-2 px-3 rounded hover:bg-gray-100">
                Verifications
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Subscriptions
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Reports
              </Link>
            </li>
            <li>
              <Link href="#" className="block py-2 px-3 rounded hover:bg-gray-100">
                Settings
              </Link>
            </li>
          </ul>
        </nav>
      </aside>
      {/* main content area */}
      <div className="flex-1 flex flex-col">
        <header className="bg-white shadow flex items-center justify-between px-6 h-16">
          <div>
            {/* optionally add search or logo */}
          </div>
          <div className="flex items-center space-x-4">
            <button className="relative">
              <span className="inline-block w-5 h-5 bg-gray-300 rounded-full"></span>
            </button>
            {user && <span className="text-sm text-gray-600">{user.email}</span>}
            {user && (
              <button onClick={logout} className="px-3 py-1 bg-red-600 text-white rounded">
                Logout
              </button>
            )}
          </div>
        </header>
        <main className="p-6 overflow-auto">{children}</main>
      </div>
    </div>
  );
};

export default AdminLayout;

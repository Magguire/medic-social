import React from 'react';

interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  type?: 'success' | 'error' | 'warning' | 'info';
  title?: string;
  message: string;
  onClose?: () => void;
  dismissible?: boolean;
}

export const Alert: React.FC<AlertProps> = ({
  type = 'info',
  title,
  message,
  onClose,
  dismissible = true,
}) => {
  const typeClasses = {
    success: 'bg-green-50 border-green-200 text-green-800',
    error: 'bg-red-50 border-red-200 text-red-800',
    warning: 'bg-yellow-50 border-yellow-200 text-yellow-800',
    info: 'bg-blue-50 border-blue-200 text-blue-800',
  };

  const iconClasses = {
    success: '✓',
    error: '✕',
    warning: '!',
    info: 'ⓘ',
  };

  return (
    <div className={`border-l-4 p-4 rounded-lg flex justify-between items-start ${typeClasses[type]}`}>
      <div className="flex gap-3">
        <span className="font-bold text-lg">{iconClasses[type]}</span>
        <div>
          {title && <h3 className="font-semibold">{title}</h3>}
          <p>{message}</p>
        </div>
      </div>
      {dismissible && onClose && (
        <button onClick={onClose} className="text-xl font-bold opacity-50 hover:opacity-100">
          ×
        </button>
      )}
    </div>
  );
};

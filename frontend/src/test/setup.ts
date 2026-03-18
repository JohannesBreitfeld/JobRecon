import '@testing-library/jest-dom';
import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import React from 'react';

// Mock MUI icons to avoid file handle issues on Windows
vi.mock('@mui/icons-material', () => ({
  Visibility: () => React.createElement('span', { 'data-testid': 'visibility-icon' }),
  VisibilityOff: () => React.createElement('span', { 'data-testid': 'visibility-off-icon' }),
  AccountCircle: () => React.createElement('span', { 'data-testid': 'account-circle-icon' }),
  Work: () => React.createElement('span', { 'data-testid': 'work-icon' }),
  Search: () => React.createElement('span', { 'data-testid': 'search-icon' }),
  Notifications: () => React.createElement('span', { 'data-testid': 'notifications-icon' }),
  Menu: () => React.createElement('span', { 'data-testid': 'menu-icon' }),
}));

// Cleanup after each test
afterEach(() => {
  cleanup();
  localStorage.clear();
  vi.clearAllMocks();
});

// Mock fetch globally
global.fetch = vi.fn();

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
  length: 0,
  key: vi.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

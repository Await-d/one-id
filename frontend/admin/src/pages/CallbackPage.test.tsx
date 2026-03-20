import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

vi.mock('antd', async () => {
  const actual = await vi.importActual('antd');
  return { ...actual, message: { success: vi.fn(), error: vi.fn() } };
});

const mockHandleCallback = vi.fn();
vi.mock('../lib/oidcConfig', () => ({
  handleCallback: () => mockHandleCallback(),
  userManager: { signinRedirect: vi.fn(), signinSilent: vi.fn() },
  getUser: vi.fn(),
}));

import { CallbackPage } from './CallbackPage';

describe('CallbackPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading spinner while processing callback', () => {
    mockHandleCallback.mockReturnValue(new Promise(() => {}));
    render(
      <MemoryRouter>
        <CallbackPage />
      </MemoryRouter>
    );
    expect(document.body).toBeTruthy();
  });

  it('navigates to home on successful callback', async () => {
    mockHandleCallback.mockResolvedValue(undefined);
    render(
      <MemoryRouter>
        <CallbackPage />
      </MemoryRouter>
    );
    await vi.waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });

  it('navigates to login on failed callback', async () => {
    mockHandleCallback.mockRejectedValue(new Error('callback error'));
    render(
      <MemoryRouter>
        <CallbackPage />
      </MemoryRouter>
    );
    await vi.waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/login');
    });
  });
});

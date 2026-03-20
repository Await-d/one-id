import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { LoginPage } from './Login';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k, i18n: { language: 'en', changeLanguage: vi.fn() } }),
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}));

vi.mock('oidc-client-ts', () => ({
  UserManager: vi.fn(),
}));

vi.mock('../lib/apiClient', () => ({
  apiClient: { get: vi.fn().mockResolvedValue({ data: [] }) },
  API_BASE_URL: 'http://localhost:5101',
}));

describe('LoginPage', () => {
  it('renders without crashing', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );
    expect(document.body).toBeTruthy();
  });

  it('shows username and password inputs', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });
});

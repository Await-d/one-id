import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => vi.fn() };
});

vi.mock('../lib/usersApi', () => ({
  usersApi: {
    getUsers: vi.fn().mockResolvedValue([]),
    getRoles: vi.fn().mockResolvedValue([]),
  },
}));

vi.mock('antd', async () => {
  const actual = await vi.importActual('antd');
  return { ...actual, message: { success: vi.fn(), error: vi.fn() } };
});

import { UsersPage } from './UsersPage';

const createClient = () => new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

describe('UsersPage', () => {
  it('renders without crashing', () => {
    render(
      <QueryClientProvider client={createClient()}>
        <MemoryRouter>
          <UsersPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
    expect(document.body).toBeTruthy();
  });

  it('shows a table or loading state', () => {
    render(
      <QueryClientProvider client={createClient()}>
        <MemoryRouter>
          <UsersPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
    expect(document.querySelector('.ant-table, .ant-spin')).toBeTruthy();
  });
});

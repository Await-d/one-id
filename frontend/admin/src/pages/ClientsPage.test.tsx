import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}));

vi.mock('../lib/clientsApi', () => ({
  clientsApi: {
    getClients: vi.fn().mockResolvedValue([]),
  },
}));

vi.mock('antd', async () => {
  const actual = await vi.importActual('antd');
  return { ...actual, message: { success: vi.fn(), error: vi.fn() } };
});

import { ClientsPage } from './ClientsPage';

const createClient = () => new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

describe('ClientsPage', () => {
  it('renders without crashing', () => {
    render(
      <QueryClientProvider client={createClient()}>
        <MemoryRouter>
          <ClientsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
    expect(document.body).toBeTruthy();
  });

  it('shows table component', () => {
    render(
      <QueryClientProvider client={createClient()}>
        <MemoryRouter>
          <ClientsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
    expect(document.querySelector('.ant-table, .ant-spin, .ant-card')).toBeTruthy();
  });
});

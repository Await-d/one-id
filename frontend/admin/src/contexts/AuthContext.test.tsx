import { render, screen, waitFor, act } from "@testing-library/react";
import { vi, describe, it, expect, beforeEach } from "vitest";
import React from "react";
import { AuthProvider, useAuth } from "./AuthContext";

type HandlerFn = (...args: unknown[]) => void;
type EventKey = "userLoaded" | "userUnloaded" | "accessTokenExpiring" | "accessTokenExpired";
const eventHandlers: Partial<Record<EventKey, HandlerFn[]>> = {};

const {
  mockSigninSilent,
  mockSigninRedirect,
  mockGetUserFn,
  mockEvents,
} = vi.hoisted(() => {
  const handlers: Partial<Record<string, HandlerFn[]>> = {};
  const addHandler = (key: string) =>
    vi.fn((fn: HandlerFn) => {
      if (!handlers[key]) handlers[key] = [];
      handlers[key]!.push(fn);
    });
  return {
    mockSigninSilent: vi.fn(),
    mockSigninRedirect: vi.fn(),
    mockGetUserFn: vi.fn(),
    mockEvents: {
      addUserLoaded: addHandler("userLoaded"),
      addUserUnloaded: addHandler("userUnloaded"),
      addAccessTokenExpiring: addHandler("accessTokenExpiring"),
      addAccessTokenExpired: addHandler("accessTokenExpired"),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
      removeAccessTokenExpiring: vi.fn(),
      removeAccessTokenExpired: vi.fn(),
      _handlers: handlers,
    },
  };
});

vi.mock("../lib/oidcConfig", () => ({
  userManager: {
    signinSilent: mockSigninSilent,
    signinRedirect: mockSigninRedirect,
    signoutRedirect: vi.fn(),
    getUser: vi.fn(),
    removeUser: vi.fn(),
    events: mockEvents,
  },
  getUser: mockGetUserFn,
}));

const fireEvent = async (key: EventKey) => {
  const fns = (mockEvents._handlers as Record<string, HandlerFn[]>)[key] ?? [];
  for (const fn of fns) await fn();
};

const AuthConsumer = () => {
  const { isLoading, isAuthenticated } = useAuth();
  return (
    <div>
      <span data-testid="loading">{isLoading ? "loading" : "done"}</span>
      <span data-testid="auth">{isAuthenticated ? "yes" : "no"}</span>
    </div>
  );
};

const renderProvider = () =>
  render(
    <AuthProvider>
      <AuthConsumer />
    </AuthProvider>
  );

describe("AuthProvider", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const key of Object.keys(mockEvents._handlers)) {
      delete (mockEvents._handlers as Record<string, HandlerFn[]>)[key];
    }
  });

  it("starts in loading state then resolves", async () => {
    mockGetUserFn.mockResolvedValueOnce(null);
    renderProvider();
    expect(screen.getByTestId("loading").textContent).toBe("loading");
    await waitFor(() =>
      expect(screen.getByTestId("loading").textContent).toBe("done")
    );
  });

  it("calls signinSilent when access token is expiring", async () => {
    mockGetUserFn.mockResolvedValueOnce(null);
    mockSigninSilent.mockResolvedValueOnce(undefined);
    renderProvider();
    await waitFor(() =>
      expect(screen.getByTestId("loading").textContent).toBe("done")
    );
    await act(async () => { await fireEvent("accessTokenExpiring"); });
    expect(mockSigninSilent).toHaveBeenCalledOnce();
    expect(mockSigninRedirect).not.toHaveBeenCalled();
  });

  it("calls signinRedirect when signinSilent fails", async () => {
    mockGetUserFn.mockResolvedValueOnce(null);
    mockSigninSilent.mockRejectedValueOnce(new Error("silent failed"));
    mockSigninRedirect.mockResolvedValueOnce(undefined);
    renderProvider();
    await waitFor(() =>
      expect(screen.getByTestId("loading").textContent).toBe("done")
    );
    await act(async () => { await fireEvent("accessTokenExpiring"); });
    expect(mockSigninSilent).toHaveBeenCalledOnce();
    expect(mockSigninRedirect).toHaveBeenCalledOnce();
  });
});

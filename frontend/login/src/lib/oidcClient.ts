import { UserManager, WebStorageStateStore } from "oidc-client-ts";

// 生产环境使用window.location.origin，开发环境使用localhost:5101
const authority = import.meta.env.VITE_OIDC_AUTHORITY && import.meta.env.VITE_OIDC_AUTHORITY.trim()
  ? import.meta.env.VITE_OIDC_AUTHORITY
  : (import.meta.env.DEV ? "http://localhost:5101" : window.location.origin);
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID ?? "spa.portal";
const redirectUri = import.meta.env.VITE_OIDC_REDIRECT_URI ?? `${window.location.origin}/callback`;
const postLogoutRedirectUri = import.meta.env.VITE_OIDC_LOGOUT_REDIRECT_URI ?? window.location.origin;

export const userManager = new UserManager({
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: "code",
  scope: import.meta.env.VITE_OIDC_SCOPE ?? "openid profile email offline_access",
  loadUserInfo: true,
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage }),
});

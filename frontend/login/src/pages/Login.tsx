import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher } from "../components/LanguageSwitcher";
import { apiClient, API_BASE_URL } from "../lib/apiClient";

interface ExternalProvider {
  name: string;
  displayName?: string;
}

export function LoginPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [providers, setProviders] = useState<ExternalProvider[]>([]);
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [registrationEnabled, setRegistrationEnabled] = useState(true); // 默认启用

  useEffect(() => {
    loadProviders();
    loadRegistrationStatus();
  }, []);

  const loadProviders = async () => {
    try {
      const data = await apiClient.get<ExternalProvider[]>("/api/externalauth/providers");
      setProviders(
        (data ?? []).map((provider) => ({
          name: provider.name,
          displayName: provider.displayName || provider.name,
        }))
      );
    } catch (error) {
      console.error("Failed to load external providers", error);
    }
  };

  const loadRegistrationStatus = async () => {
    try {
      const enabled = await apiClient.get<boolean>("/api/account/registration-enabled");
      setRegistrationEnabled(enabled ?? true);
    } catch (error) {
      console.error("Failed to load registration status", error);
      // 失败时默认启用注册
      setRegistrationEnabled(true);
    }
  };

  const handleLocalLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const data = await apiClient.post<{ success: boolean; requiresTwoFactor?: boolean; message?: string }>(
        "/api/account/login",
        {
          userName: username,
          password: password,
          rememberMe: false,
        }
      );

      if (data.success) {
        if (data.requiresTwoFactor) {
          // 需要2FA验证，传递 returnUrl
          const returnUrl = new URLSearchParams(window.location.search).get("returnUrl");
          if (returnUrl) {
            navigate(`/two-factor?returnUrl=${encodeURIComponent(returnUrl)}`);
          } else {
            navigate("/two-factor");
          }
        } else {
          // 直接登录成功，检查是否有 returnUrl
          const returnUrl = new URLSearchParams(window.location.search).get("returnUrl");
          if (returnUrl) {
            // 有 returnUrl，重定向回去（通常是 OIDC 授权端点）
            window.location.href = returnUrl;
          } else {
            // 没有 returnUrl，跳转到默认的授权页面
            window.location.href = "/signin";
          }
        }
      } else {
        setError(data.message || t('login.loginFailed'));
      }
    } catch (error) {
      console.error("Login failed", error);
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  const handleExternalLogin = (providerName: string) => {
    const returnUrl = new URLSearchParams(window.location.search).get("returnUrl") || "/";
    window.location.href = `${API_BASE_URL}/api/externalauth/challenge/${providerName}?returnUrl=${encodeURIComponent(returnUrl)}`;
  };

  const getProviderIcon = (name: string) => {
    switch (name.toLowerCase()) {
      case "github":
        return (
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z" />
          </svg>
        );
      case "google":
        return (
          <svg className="w-5 h-5" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
          </svg>
        );
      case "gitee":
        return (
          <svg className="w-5 h-5" fill="#C71D23" viewBox="0 0 24 24">
            <path d="M12 2.247c-5.523 0-10 4.477-10 10 0 4.418 2.865 8.166 6.839 9.489.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482C19.137 20.107 22 16.359 22 11.947c0-5.523-4.477-10-10-10z" />
          </svg>
        );
      case "wechat":
        return (
          <svg className="w-5 h-5" viewBox="0 0 48 48" fill="none">
            <path
              d="M16 20c0-6.075 5.373-11 12-11s12 4.925 12 11c0 6.076-5.373 11-12 11-.826 0-1.634-.074-2.417-.214L20 34l1.27-4.233C18.38 27.972 16 24.31 16 20Z"
              fill="#09bb07"
            />
            <circle cx="22" cy="20" r="2" fill="white" />
            <circle cx="30" cy="20" r="2" fill="white" />
            <circle cx="26" cy="26" r="2" fill="white" />
          </svg>
        );
      default:
        return (
          <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-gray-200 text-xs font-semibold text-gray-600">
            {name.slice(0, 1).toUpperCase()}
          </span>
        );
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* 语言切换器 */}
        <div className="flex justify-end">
          <LanguageSwitcher />
        </div>

        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('login.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('login.subtitle')}
          </p>
        </div>

        <div className="mt-8 space-y-6">
          {/* 本地登录表单 */}
          <form onSubmit={handleLocalLogin} className="space-y-4">
            {error && (
              <div className="rounded-md bg-red-50 p-4">
                <p className="text-sm text-red-800">{error}</p>
              </div>
            )}

            <div>
              <label htmlFor="username" className="sr-only">
                {t('login.usernameLabel')}
              </label>
              <input
                id="username"
                name="username"
                type="text"
                required
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className="appearance-none relative block w-full px-3 py-3 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10"
                placeholder={t('login.usernamePlaceholder')}
              />
            </div>

            <div>
              <label htmlFor="password" className="sr-only">
                {t('login.passwordLabel')}
              </label>
              <input
                id="password"
                name="password"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="appearance-none relative block w-full px-3 py-3 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10"
                placeholder={t('login.passwordPlaceholder')}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:opacity-50"
            >
              {loading ? t('login.loggingIn') : t('login.loginButton')}
            </button>

            <div className="text-sm text-center">
              <a href="/forgot-password" className="font-medium text-blue-600 hover:text-blue-500">
                {t('login.forgotPassword')}
              </a>
            </div>
          </form>

          {/* 外部登录 */}
          {providers.length > 0 && (
            <>
              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <div className="w-full border-t border-gray-300"></div>
                </div>
                <div className="relative flex justify-center text-sm">
                  <span className="px-2 bg-gray-50 text-gray-500">{t('login.orContinueWith')}</span>
                </div>
              </div>

              <div className="space-y-2">
                {providers.map((provider) => (
                  <button
                    key={provider.name}
                    onClick={() => handleExternalLogin(provider.name)}
                    className="w-full flex items-center justify-center px-4 py-3 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                  >
                    {getProviderIcon(provider.name)}
                    <span className="ml-2">{provider.displayName}</span>
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        {registrationEnabled && (
          <div className="text-center mt-6">
            <p className="text-sm text-gray-600">
              {t('login.noAccount')}{' '}
              <a href="/register" className="font-medium text-blue-600 hover:text-blue-500">
                {t('login.registerLink')}
              </a>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

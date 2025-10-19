import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher } from "../components/LanguageSwitcher";

interface ExternalLogin {
  loginProvider: string;
  providerKey: string;
  providerDisplayName: string;
}

interface ExternalProvider {
  name: string;
  displayName: string;
}

export function ProfilePage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [user, setUser] = useState<any>(null);
  const [logins, setLogins] = useState<ExternalLogin[]>([]);
  const [providers, setProviders] = useState<ExternalProvider[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadUserInfo();
    loadExternalLogins();
    loadProviders();
  }, []);

  const loadUserInfo = async () => {
    try {
      const apiUrl = import.meta.env.DEV
        ? "http://localhost:5101/api/account/me"
        : "/api/account/me";

      const response = await fetch(apiUrl, {
        credentials: "include",
      });

      if (response.ok) {
        const data = await response.json();
        setUser(data);
      } else if (response.status === 401) {
        navigate("/login");
      }
    } catch (error) {
      console.error("Failed to load user info", error);
    }
  };

  const loadExternalLogins = async () => {
    try {
      const apiUrl = import.meta.env.DEV
        ? "http://localhost:5101/api/externalauth/logins"
        : "/api/externalauth/logins";

      const response = await fetch(apiUrl, {
        credentials: "include",
      });

      if (response.ok) {
        const data = await response.json();
        setLogins(data);
      }
    } catch (error) {
      console.error("Failed to load external logins", error);
    } finally {
      setLoading(false);
    }
  };

  const loadProviders = async () => {
    try {
      const apiUrl = import.meta.env.DEV
        ? "http://localhost:5101/api/externalauth/providers"
        : "/api/externalauth/providers";

      const response = await fetch(apiUrl, {
        credentials: "include",
      });

      if (response.ok) {
        const data = await response.json();
        setProviders(data);
      }
    } catch (error) {
      console.error("Failed to load providers", error);
    }
  };

  const handleLinkAccount = (providerName: string) => {
    const apiUrl = import.meta.env.DEV
      ? `http://localhost:5101/api/externalauth/link/${providerName}`
      : `/api/externalauth/link/${providerName}`;

    window.location.href = apiUrl;
  };

  const handleUnlinkAccount = async (provider: string, providerKey: string) => {
    if (!confirm(t('profile.confirmUnlink', { provider }))) {
      return;
    }

    try {
      const apiUrl = import.meta.env.DEV
        ? `http://localhost:5101/api/externalauth/unlink/${provider}/${providerKey}`
        : `/api/externalauth/unlink/${provider}/${providerKey}`;

      const response = await fetch(apiUrl, {
        method: "DELETE",
        credentials: "include",
      });

      if (response.ok) {
        alert(t('profile.unlinkSuccess'));
        loadExternalLogins();
      } else {
        alert(t('profile.unlinkFailed'));
      }
    } catch (error) {
      console.error("Failed to unlink account", error);
      alert(t('profile.unlinkFailed'));
    }
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
      default:
        return null;
    }
  };

  const isLinked = (providerName: string) => {
    return logins.some(l => l.loginProvider.toLowerCase() === providerName.toLowerCase());
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 via-white to-purple-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">{t('profile.loading')}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{t('profile.title')}</h1>
            <p className="mt-2 text-sm text-gray-600">{t('profile.basicInfo')}</p>
          </div>
          <LanguageSwitcher />
        </div>

        <div className="space-y-6">
          {/* User Information Card */}
          <div className="bg-white shadow-lg rounded-xl overflow-hidden border border-gray-100">
            <div className="px-6 py-5 border-b border-gray-200 bg-gradient-to-r from-blue-500 to-purple-500">
              <h3 className="text-lg font-semibold text-white">{t('profile.basicInfo')}</h3>
            </div>
            <div className="px-6 py-6">
              {user && (
                <dl className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                  <div className="bg-gray-50 rounded-lg p-4">
                    <dt className="text-sm font-medium text-gray-500 mb-1">{t('profile.username')}</dt>
                    <dd className="text-base font-semibold text-gray-900">{user.userName}</dd>
                  </div>
                  <div className="bg-gray-50 rounded-lg p-4">
                    <dt className="text-sm font-medium text-gray-500 mb-1">{t('profile.email')}</dt>
                    <dd className="text-base font-semibold text-gray-900 flex items-center">
                      {user.email}
                      {user.emailConfirmed && (
                        <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                          ✓ {t('profile.emailConfirmed')}
                        </span>
                      )}
                    </dd>
                  </div>
                  <div className="bg-gray-50 rounded-lg p-4 sm:col-span-2">
                    <dt className="text-sm font-medium text-gray-500 mb-1">{t('profile.displayName')}</dt>
                    <dd className="text-base font-semibold text-gray-900">{user.displayName || "-"}</dd>
                  </div>
                </dl>
              )}
            </div>
          </div>

          {/* Security Settings Card */}
          <div className="bg-white shadow-lg rounded-xl overflow-hidden border border-gray-100">
            <div className="px-6 py-5 border-b border-gray-200 bg-gradient-to-r from-purple-500 to-pink-500">
              <h3 className="text-lg font-semibold text-white">{t('profile.securitySettings')}</h3>
            </div>
            <div className="px-6 py-6">
              <div className="space-y-4">
                {/* MFA Setting */}
                <div className="flex items-center justify-between p-5 border-2 border-gray-200 rounded-lg hover:border-blue-300 transition-all hover:shadow-md">
                  <div className="flex items-center space-x-4">
                    <div className="flex-shrink-0">
                      <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                        <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                        </svg>
                      </div>
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-gray-900">{t('profile.twoFactorAuth')}</p>
                      <p className="text-xs text-gray-500 mt-1">{t('profile.twoFactorAuthDesc')}</p>
                    </div>
                  </div>
                  <button
                    onClick={() => navigate("/mfa-setup")}
                    className="px-5 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors shadow-sm hover:shadow-md"
                  >
                    {t('profile.manage')}
                  </button>
                </div>

                {/* API Keys Setting */}
                <div className="flex items-center justify-between p-5 border-2 border-gray-200 rounded-lg hover:border-purple-300 transition-all hover:shadow-md">
                  <div className="flex items-center space-x-4">
                    <div className="flex-shrink-0">
                      <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                        <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                        </svg>
                      </div>
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-gray-900">{t('nav.apiKeys')}</p>
                      <p className="text-xs text-gray-500 mt-1">{t('profile.apiKeysDesc')}</p>
                    </div>
                  </div>
                  <button
                    onClick={() => navigate("/api-keys")}
                    className="px-5 py-2.5 text-sm font-medium text-white bg-purple-600 hover:bg-purple-700 rounded-lg transition-colors shadow-sm hover:shadow-md"
                  >
                    {t('profile.manage')}
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* External Accounts Card */}
          <div className="bg-white shadow-lg rounded-xl overflow-hidden border border-gray-100">
            <div className="px-6 py-5 border-b border-gray-200 bg-gradient-to-r from-green-500 to-teal-500">
              <h3 className="text-lg font-semibold text-white">{t('profile.linkedAccounts')}</h3>
            </div>
            <div className="px-6 py-6">
              <div className="space-y-3">
                {providers.map((provider) => {
                  const linked = isLinked(provider.name);
                  const loginInfo = logins.find(l => l.loginProvider.toLowerCase() === provider.name.toLowerCase());

                  return (
                    <div key={provider.name} className="flex items-center justify-between p-5 border-2 border-gray-200 rounded-lg hover:border-green-300 transition-all hover:shadow-md">
                      <div className="flex items-center space-x-4">
                        <div className="flex-shrink-0 w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center">
                          {getProviderIcon(provider.name)}
                        </div>
                        <div>
                          <p className="text-sm font-semibold text-gray-900">{provider.displayName}</p>
                          {linked && loginInfo && (
                            <p className="text-xs text-green-600 mt-1">✓ {t('profile.linked')}</p>
                          )}
                        </div>
                      </div>
                      {linked && loginInfo ? (
                        <button
                          onClick={() => handleUnlinkAccount(loginInfo.loginProvider, loginInfo.providerKey)}
                          className="px-5 py-2.5 text-sm font-medium text-red-600 hover:text-red-700 border-2 border-red-300 rounded-lg hover:bg-red-50 transition-all shadow-sm hover:shadow-md"
                        >
                          {t('profile.unlink')}
                        </button>
                      ) : (
                        <button
                          onClick={() => handleLinkAccount(provider.name)}
                          className="px-5 py-2.5 text-sm font-medium text-green-600 hover:text-green-700 border-2 border-green-300 rounded-lg hover:bg-green-50 transition-all shadow-sm hover:shadow-md"
                        >
                          {t('profile.link')}
                        </button>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="bg-white shadow-lg rounded-xl overflow-hidden border border-gray-100 px-6 py-5">
            <div className="flex flex-col sm:flex-row justify-between items-center gap-4">
              <button
                onClick={() => navigate("/")}
                className="w-full sm:w-auto px-6 py-3 text-sm font-medium text-gray-700 hover:text-gray-900 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
              >
                ← {t('profile.backToHome')}
              </button>
              <button
                onClick={async () => {
                  const apiUrl = import.meta.env.DEV
                    ? "http://localhost:5101/api/account/logout"
                    : "/api/account/logout";
                  await fetch(apiUrl, { method: "POST", credentials: "include" });
                  navigate("/login");
                }}
                className="w-full sm:w-auto px-6 py-3 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors shadow-sm hover:shadow-md"
              >
                {t('common.logout')}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

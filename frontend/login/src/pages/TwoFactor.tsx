import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { apiClient } from '../lib/apiClient';

export default function TwoFactorPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { t } = useTranslation();
  const [code, setCode] = useState('');
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const data = await apiClient.post<{ success: boolean; message?: string }>('/api/account/login-2fa', {
        code: code.trim(),
        rememberMe: false,
        isRecoveryCode: useRecoveryCode,
      });

      if (data.success) {
        // 登录成功，跳转到OIDC授权流程
        const returnUrl = new URLSearchParams(location.search).get('returnUrl') || '/signin';
        window.location.href = returnUrl;
      } else {
        setError(data.message || t('twoFactor.invalidCode'));
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
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
            {t('twoFactor.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('twoFactor.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-md bg-red-50 p-4">
              <p className="text-sm text-red-800">{error}</p>
            </div>
          )}

          <div>
            <label htmlFor="code" className="sr-only">
              {useRecoveryCode ? t('twoFactor.recoveryCodeLabel') : t('twoFactor.codeLabel')}
            </label>
            <input
              id="code"
              name="code"
              type="text"
              required
              value={code}
              onChange={(e) => {
                if (useRecoveryCode) {
                  setCode(e.target.value);
                } else {
                  // Only allow digits for TOTP
                  setCode(e.target.value.replace(/\D/g, ''));
                }
              }}
              maxLength={useRecoveryCode ? 20 : 6}
              className="appearance-none relative block w-full px-3 py-3 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 text-center text-2xl tracking-widest"
              placeholder={useRecoveryCode ? t('twoFactor.recoveryCodePlaceholder') : t('twoFactor.codePlaceholder')}
              autoComplete="off"
              autoFocus
            />
            {!useRecoveryCode && (
              <p className="mt-2 text-xs text-gray-500 text-center">
                {t('mfa.verificationCodeDesc')}
              </p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={loading || code.length < (useRecoveryCode ? 8 : 6)}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? t('twoFactor.verifying') : t('twoFactor.verifyButton')}
            </button>
          </div>

          <div className="text-center">
            <button
              type="button"
              onClick={() => {
                setUseRecoveryCode(!useRecoveryCode);
                setCode('');
                setError('');
              }}
              className="text-sm text-blue-600 hover:text-blue-500"
            >
              {useRecoveryCode
                ? t('twoFactor.useAuthenticator')
                : t('twoFactor.useRecoveryCode')}
            </button>
          </div>

          <div className="text-center">
            <button
              type="button"
              onClick={() => navigate('/login')}
              className="text-sm text-gray-600 hover:text-gray-500"
            >
              ← {t('forgotPassword.backToLogin')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from '../components/LanguageSwitcher';

interface MfaStatus {
  enabled: boolean;
  hasRecoveryCodes: boolean;
}

interface EnableMfaResponse {
  secret: string;
  qrCodeUrl: string;
  recoveryCodes: string[];
}

export default function MfaSetup() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [step, setStep] = useState<'status' | 'enable' | 'verify' | 'codes'>('status');
  const [password, setPassword] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [mfaData, setMfaData] = useState<EnableMfaResponse | null>(null);
  const [showRecoveryCodes, setShowRecoveryCodes] = useState(false);

  useEffect(() => {
    fetchStatus();
  }, []);

  const fetchStatus = async () => {
    try {
      const response = await fetch('/api/mfa/status', {
        credentials: 'include'
      });
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  const handleEnable = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await fetch('/api/mfa/enable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ password })
      });

      if (response.ok) {
        const data = await response.json();
        setMfaData(data);
        setPassword('');
        setStep('verify');
      } else {
        const errorData = await response.json();
        setError(errorData.error || t('errors.serverError'));
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await fetch('/api/mfa/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ code: verificationCode })
      });

      if (response.ok) {
        setVerificationCode('');
        setStep('codes');
      } else {
        const errorData = await response.json();
        setError(errorData.error || t('twoFactor.invalidCode'));
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  const handleDisable = async () => {
    const password = prompt(t('mfa.enterPasswordToDisable'));
    if (!password) return;

    setLoading(true);
    try {
      const response = await fetch('/api/mfa/disable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ password })
      });

      if (response.ok) {
        await fetchStatus();
        setStep('status');
      } else {
        const errorData = await response.json();
        setError(errorData.error || t('errors.serverError'));
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  const handleRegenerateCodes = async () => {
    const password = prompt(t('mfa.enterPasswordToRegenerate'));
    if (!password) return;

    setLoading(true);
    try {
      const response = await fetch('/api/mfa/regenerate-recovery-codes', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ password })
      });

      if (response.ok) {
        const codes = await response.json();
        setMfaData({ ...mfaData!, recoveryCodes: codes });
        setShowRecoveryCodes(true);
      } else {
        const errorData = await response.json();
        setError(errorData.error || t('errors.serverError'));
      }
    } catch (err) {
      setError(t('errors.networkError'));
    } finally {
      setLoading(false);
    }
  };

  if (loading && !status) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 via-white to-purple-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">{t('common.loading')}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="mb-8 flex justify-between items-start">
          <button
            onClick={() => navigate('/profile')}
            className="flex items-center text-blue-600 hover:text-blue-700 font-medium transition-colors"
          >
            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            {t('mfa.backToProfile')}
          </button>
          <LanguageSwitcher />
        </div>

        <div className="bg-white rounded-2xl shadow-2xl px-8 py-10 border border-gray-100">
          <div className="mb-8 text-center">
            <div className="flex justify-center mb-4">
              <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
                <svg className="w-10 h-10 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
            </div>
            <h2 className="text-3xl font-bold text-gray-900">
              {t('mfa.twoFactorAuthentication')}
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              {t('mfa.addExtraSecurity')}
            </p>
          </div>

          {error && (
            <div className="mb-6 p-4 bg-red-50 border-2 border-red-200 rounded-xl">
              <p className="text-sm text-red-800 font-medium">{error}</p>
            </div>
          )}

          {step === 'status' && status && (
            <div>
              {status.enabled ? (
                <div className="space-y-4">
                  <div className="p-5 bg-green-50 border-2 border-green-200 rounded-xl">
                    <p className="text-sm text-green-800 font-semibold">
                      {t('mfa.twoFactorEnabled')}
                    </p>
                  </div>
                  <div className="space-y-3">
                    <button
                      onClick={handleRegenerateCodes}
                      disabled={loading}
                      className="w-full px-6 py-3.5 bg-blue-600 text-white font-medium rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-all shadow-md hover:shadow-lg"
                    >
                      {t('mfa.regenerate')}
                    </button>
                    <button
                      onClick={handleDisable}
                      disabled={loading}
                      className="w-full px-6 py-3.5 bg-red-600 text-white font-medium rounded-xl hover:bg-red-700 disabled:opacity-50 transition-all shadow-md hover:shadow-lg"
                    >
                      {t('mfa.disable')}
                    </button>
                  </div>

                  {showRecoveryCodes && mfaData?.recoveryCodes && (
                    <div className="mt-6 p-5 bg-yellow-50 border-2 border-yellow-200 rounded-xl">
                      <h3 className="font-semibold text-yellow-900 mb-2">{t('mfa.newRecoveryCodes')}</h3>
                      <p className="text-sm text-yellow-800 mb-4">
                        {t('mfa.saveInSafePlace')}
                      </p>
                      <div className="grid grid-cols-2 gap-2 font-mono text-sm">
                        {mfaData.recoveryCodes.map((code, idx) => (
                          <div key={idx} className="bg-white p-3 rounded-lg border-2 border-yellow-300 text-center">
                            {code}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <div>
                  <p className="text-gray-700 mb-6 leading-relaxed">
                    {t('mfa.twoFactorDescription')}
                  </p>
                  <button
                    onClick={() => setStep('enable')}
                    className="w-full px-6 py-3.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-medium rounded-xl hover:from-blue-700 hover:to-purple-700 transition-all shadow-lg hover:shadow-xl"
                  >
                    {t('mfa.enableButton')}
                  </button>
                </div>
              )}
            </div>
          )}

          {step === 'enable' && (
            <form onSubmit={handleEnable} className="space-y-6">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">
                  {t('mfa.confirmPassword')}
                </label>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  className="w-full px-4 py-3 border-2 border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                  placeholder={t('mfa.enterPassword')}
                />
              </div>
              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => setStep('status')}
                  className="flex-1 px-6 py-3 border-2 border-gray-300 rounded-xl hover:bg-gray-50 font-medium transition-all"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={loading}
                  className="flex-1 px-6 py-3 bg-blue-600 text-white font-medium rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-all shadow-md hover:shadow-lg"
                >
                  {t('mfa.continue')}
                </button>
              </div>
            </form>
          )}

          {step === 'verify' && mfaData && (
            <div className="space-y-6">
              <div className="text-center">
                <h3 className="text-xl font-semibold mb-6">{t('mfa.scanQrCodeTitle')}</h3>
                <div className="inline-block p-4 bg-white border-4 border-gray-200 rounded-2xl shadow-lg">
                  <img
                    src={`/api/mfa/qrcode`}
                    alt="QR Code"
                    className="w-64 h-64"
                  />
                </div>
                <p className="mt-6 text-sm text-gray-600">
                  {t('mfa.scanQrCodeDesc')}
                </p>
                <details className="mt-4">
                  <summary className="text-sm text-blue-600 cursor-pointer font-medium hover:text-blue-700">
                    {t('mfa.cantScan')}
                  </summary>
                  <code className="mt-3 block bg-gray-100 p-3 rounded-lg text-xs break-all border border-gray-300">
                    {mfaData.secret}
                  </code>
                </details>
              </div>

              <form onSubmit={handleVerify} className="space-y-6">
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-2">
                    {t('mfa.verificationCode')}
                  </label>
                  <input
                    type="text"
                    value={verificationCode}
                    onChange={(e) => setVerificationCode(e.target.value.replace(/\D/g, ''))}
                    maxLength={6}
                    required
                    className="w-full px-4 py-4 border-2 border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-center text-3xl tracking-[0.5em] font-mono transition-all"
                    placeholder="000000"
                  />
                  <p className="mt-2 text-xs text-gray-500 text-center">
                    {t('mfa.verificationCodeDesc')}
                  </p>
                </div>
                <button
                  type="submit"
                  disabled={loading || verificationCode.length !== 6}
                  className="w-full px-6 py-3.5 bg-blue-600 text-white font-medium rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-all shadow-md hover:shadow-lg"
                >
                  {t('mfa.verifyAndEnable')}
                </button>
              </form>
            </div>
          )}

          {step === 'codes' && mfaData && (
            <div className="space-y-6">
              <div className="p-5 bg-green-50 border-2 border-green-200 rounded-xl">
                <p className="text-sm text-green-800 font-semibold">
                  {t('mfa.twoFactorEnabledSuccess')}
                </p>
              </div>

              <div className="p-6 bg-yellow-50 border-2 border-yellow-200 rounded-xl">
                <h3 className="font-semibold text-yellow-900 mb-2">{t('mfa.recoveryCodesTitle')}</h3>
                <p className="text-sm text-yellow-800 mb-4">
                  {t('mfa.recoveryCodesDesc')}
                </p>
                <div className="grid grid-cols-2 gap-3 font-mono text-sm mb-4">
                  {mfaData.recoveryCodes.map((code, idx) => (
                    <div key={idx} className="bg-white p-3 rounded-lg border-2 border-yellow-300 text-center font-semibold">
                      {code}
                    </div>
                  ))}
                </div>
                <button
                  onClick={() => {
                    const text = mfaData.recoveryCodes.join('\n');
                    navigator.clipboard.writeText(text);
                    alert(t('mfa.copiedToClipboard'));
                  }}
                  className="text-sm text-blue-600 hover:text-blue-700 font-medium flex items-center"
                >
                  <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
                  </svg>
                  {t('mfa.copyToClipboard')}
                </button>
              </div>

              <button
                onClick={() => {
                  setStep('status');
                  fetchStatus();
                }}
                className="w-full px-6 py-3.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-medium rounded-xl hover:from-blue-700 hover:to-purple-700 transition-all shadow-lg hover:shadow-xl"
              >
                {t('mfa.done')}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

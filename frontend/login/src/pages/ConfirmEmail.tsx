import { useEffect, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { API_BASE_URL as API_BASE } from '../lib/apiClient';

export default function ConfirmEmail() {
    const { t } = useTranslation();
    const [searchParams] = useSearchParams();
    const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
    const [message, setMessage] = useState('');

    useEffect(() => {
        const confirmEmail = async () => {
            const email = searchParams.get('email');
            const token = searchParams.get('token');

            if (!email || !token) {
                setStatus('error');
                setMessage(t('confirmEmail.invalidLink'));
                return;
            }

            try {
                const response = await fetch(`${API_BASE}/api/account/confirm-email`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email, token }),
                });

                const data = await response.json();

                if (response.ok && data.success) {
                    setStatus('success');
                    setMessage(data.message || t('confirmEmail.successMessage'));
                } else {
                    setStatus('error');
                    setMessage(data.message || t('confirmEmail.failedMessage'));
                }
            } catch (error) {
                setStatus('error');
                setMessage(t('errors.networkError'));
            }
        };

        confirmEmail();
    }, [searchParams, t]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500">
            <div className="max-w-md w-full mx-4">
                <div className="absolute top-6 right-6">
                    <LanguageSwitcher />
                </div>

                <div className="bg-white rounded-lg shadow-xl p-8">
                    <div className="text-center">
                        {status === 'loading' && (
                            <>
                                <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-blue-100 mb-4">
                                    <svg className="animate-spin h-8 w-8 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                </div>
                                <h2 className="text-2xl font-bold text-gray-900 mb-2">{t('confirmEmail.title')}</h2>
                                <p className="text-gray-600">{t('confirmEmail.loading')}</p>
                            </>
                        )}

                        {status === 'success' && (
                            <>
                                <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-100 mb-4">
                                    <svg className="w-8 h-8 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                    </svg>
                                </div>
                                <h2 className="text-2xl font-bold text-gray-900 mb-2">{t('confirmEmail.success')}</h2>
                                <p className="text-gray-600 mb-6">{message}</p>
                                <Link
                                    to="/login"
                                    className="inline-block bg-gradient-to-r from-indigo-500 to-purple-600 text-white font-semibold px-8 py-3 rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg"
                                >
                                    {t('confirmEmail.goToLogin')}
                                </Link>
                            </>
                        )}

                        {status === 'error' && (
                            <>
                                <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-red-100 mb-4">
                                    <svg className="w-8 h-8 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                    </svg>
                                </div>
                                <h2 className="text-2xl font-bold text-gray-900 mb-2">{t('confirmEmail.failed')}</h2>
                                <p className="text-gray-600 mb-6">{message}</p>
                                <div className="space-y-3">
                                    <Link
                                        to="/resend-confirmation"
                                        className="inline-block w-full bg-gradient-to-r from-indigo-500 to-purple-600 text-white font-semibold px-8 py-3 rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg"
                                    >
                                        {t('confirmEmail.resendLink')}
                                    </Link>
                                    <Link
                                        to="/login"
                                        className="inline-block w-full bg-gray-200 text-gray-700 font-semibold px-8 py-3 rounded-lg hover:bg-gray-300 transition-all duration-200"
                                    >
                                        {t('confirmEmail.backToLogin')}
                                    </Link>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}


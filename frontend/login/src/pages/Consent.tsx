import React, { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { submitForm, extractSearchParams, redirectWithError } from "../lib/formUtils";

interface Scope {
    name: string;
    displayName: string;
    description: string;
}

interface ConsentInfo {
    clientId: string;
    clientName: string;
    scopes: Scope[];
}

export function Consent() {
    const { t } = useTranslation();
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const [consentInfo, setConsentInfo] = useState<ConsentInfo | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const returnUrl = searchParams.get("returnUrl");

    useEffect(() => {
        const fetchConsentInfo = async () => {
            try {
                // 解析returnUrl中的参数
                if (!returnUrl) {
                    setError("Missing return URL");
                    setLoading(false);
                    return;
                }

                const url = new URL(returnUrl, window.location.origin);
                const clientId = url.searchParams.get("client_id");
                const scope = url.searchParams.get("scope");

                if (!clientId) {
                    setError("Missing client_id");
                    setLoading(false);
                    return;
                }

                // 获取consent信息
                const response = await fetch(
                    `/api/consent/info?client_id=${encodeURIComponent(clientId)}&scope=${encodeURIComponent(scope || "")}`,
                    {
                        credentials: "include",
                    }
                );

                if (!response.ok) {
                    throw new Error("Failed to fetch consent information");
                }

                const data = await response.json();
                setConsentInfo(data);
            } catch (err) {
                setError(err instanceof Error ? err.message : "An error occurred");
            } finally {
                setLoading(false);
            }
        };

        fetchConsentInfo();
    }, [returnUrl]);

    const handleAllow = () => {
        if (!returnUrl) return;

        setIsSubmitting(true);

        // 使用表单工具提交授权请求
        const url = new URL(returnUrl, window.location.origin);
        const params = extractSearchParams(url);

        // 添加consent granted标志
        params.consent_granted = "true";

        // 提交表单，浏览器会自动处理重定向
        submitForm(url.pathname + url.search, params, "POST");
    };

    const handleDeny = () => {
        // 重定向回客户端并带上错误信息
        if (returnUrl) {
            const url = new URL(returnUrl, window.location.origin);
            const redirectUri = url.searchParams.get("redirect_uri");
            if (redirectUri) {
                redirectWithError(redirectUri, "access_denied", "User denied consent");
            }
        }
    };

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-4 text-gray-600">{t('consent.loading')}</p>
                </div>
            </div>
        );
    }

    if (error || !consentInfo) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <div className="max-w-md w-full bg-white shadow-lg rounded-lg p-8">
                    <div className="text-center">
                        <svg
                            className="mx-auto h-12 w-12 text-red-500"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                        >
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                            />
                        </svg>
                        <h2 className="mt-4 text-xl font-semibold text-gray-900">{t('consent.error')}</h2>
                        <p className="mt-2 text-gray-600">{error || t('consent.errorLoadingConsent')}</p>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full">
                <div className="bg-white shadow-lg rounded-lg overflow-hidden">
                    {/* Header */}
                    <div className="bg-gradient-to-r from-blue-600 to-purple-600 px-6 py-8 text-white">
                        <div className="flex items-center justify-center mb-4">
                            <svg
                                className="h-12 w-12"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                            >
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                                />
                            </svg>
                        </div>
                        <h2 className="text-2xl font-bold text-center">{t('consent.title')}</h2>
                        <p className="mt-2 text-center text-blue-100">
                            <strong>{consentInfo.clientName}</strong> {t('consent.clientWantsAccess')}
                        </p>
                    </div>

                    {/* Scopes */}
                    <div className="px-6 py-6">
                        <p className="text-sm text-gray-600 mb-4">
                            {t('consent.permissionsRequest')}
                        </p>
                        <ul className="space-y-3">
                            {consentInfo.scopes.map((scope) => (
                                <li key={scope.name} className="flex items-start">
                                    <svg
                                        className="h-6 w-6 text-green-500 mr-3 flex-shrink-0 mt-0.5"
                                        fill="none"
                                        stroke="currentColor"
                                        viewBox="0 0 24 24"
                                    >
                                        <path
                                            strokeLinecap="round"
                                            strokeLinejoin="round"
                                            strokeWidth={2}
                                            d="M5 13l4 4L19 7"
                                        />
                                    </svg>
                                    <div>
                                        <p className="font-medium text-gray-900">{scope.displayName}</p>
                                        <p className="text-sm text-gray-600">{scope.description}</p>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>

                    {/* Actions */}
                    <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 flex gap-4">
                        <button
                            onClick={handleDeny}
                            disabled={isSubmitting}
                            className="flex-1 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-md font-medium hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {t('consent.denyButton')}
                        </button>
                        <button
                            onClick={handleAllow}
                            disabled={isSubmitting}
                            className="flex-1 bg-blue-600 text-white px-4 py-2 rounded-md font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
                        >
                            {isSubmitting ? (
                                <>
                                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                                    {t('consent.processing')}
                                </>
                            ) : (
                                t('consent.allowButton')
                            )}
                        </button>
                    </div>
                </div>

                <p className="mt-4 text-center text-xs text-gray-500">
                    {t('consent.authorizationNote', { clientName: consentInfo.clientName })}
                </p>
            </div>
        </div>
    );
}


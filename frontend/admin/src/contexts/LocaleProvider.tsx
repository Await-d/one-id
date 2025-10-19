import { ReactNode, useState, useEffect } from 'react';
import { ConfigProvider } from 'antd';
import { useTranslation } from 'react-i18next';
import { antdLocales } from '../i18n/config';

interface LocaleProviderProps {
    children: ReactNode;
}

export function LocaleProvider({ children }: LocaleProviderProps) {
    const { i18n } = useTranslation();
    const [locale, setLocale] = useState(() => {
        const lang = i18n.language || 'zh';
        return antdLocales[lang as keyof typeof antdLocales] || antdLocales.zh;
    });

    useEffect(() => {
        const handleLanguageChange = (lng: string) => {
            setLocale(antdLocales[lng as keyof typeof antdLocales] || antdLocales.zh);
        };

        i18n.on('languageChanged', handleLanguageChange);

        return () => {
            i18n.off('languageChanged', handleLanguageChange);
        };
    }, [i18n]);

    return (
        <ConfigProvider locale={locale}>
            {children}
        </ConfigProvider>
    );
}


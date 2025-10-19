import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import en from './locales/en.json';
import zh from './locales/zh.json';

i18n
    .use(LanguageDetector) // 自动检测用户语言
    .use(initReactI18next) // 连接react-i18next
    .init({
        resources: {
            en: { translation: en },
            zh: { translation: zh },
        },
        fallbackLng: 'en', // 默认语言
        supportedLngs: ['en', 'zh'], // 支持的语言
        interpolation: {
            escapeValue: false, // React已经安全处理了XSS
        },
        detection: {
            // 语言检测顺序
            order: ['localStorage', 'navigator', 'htmlTag'],
            // localStorage中的key
            lookupLocalStorage: 'i18nextLng',
            // 缓存用户语言
            caches: ['localStorage'],
        },
    });

export default i18n;


import { GlobalOutlined } from '@ant-design/icons';
import { Dropdown, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MenuProps } from 'antd';

export function LanguageSwitcher() {
    const { i18n } = useTranslation();

    const languages = [
        { code: 'zh', label: '简体中文', icon: '🇨🇳' },
        { code: 'en', label: 'English', icon: '🇺🇸' },
    ];

    const currentLanguage = languages.find(lang => lang.code === i18n.language) || languages[0];

    const items: MenuProps['items'] = languages.map((lang) => ({
        key: lang.code,
        label: (
            <Space>
                <span style={{ fontSize: '18px' }}>{lang.icon}</span>
                <span>{lang.label}</span>
            </Space>
        ),
        onClick: () => {
            i18n.changeLanguage(lang.code);
        },
    }));

    return (
        <Dropdown menu={{ items, selectedKeys: [i18n.language] }} placement="bottomRight">
            <Space style={{ cursor: 'pointer', padding: '8px 16px', borderRadius: '4px' }} className="hover:bg-gray-100">
                <GlobalOutlined style={{ fontSize: '18px' }} />
                <span>{currentLanguage.label}</span>
            </Space>
        </Dropdown>
    );
}


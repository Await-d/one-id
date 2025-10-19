import { useState } from 'react';
import {
    Table,
    Button,
    Modal,
    Form,
    Input,
    Select,
    Switch,
    message,
    Space,
    Tag,
    Popconfirm,
    InputNumber
} from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { PlusOutlined, EditOutlined, DeleteOutlined, CheckCircleOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { emailConfigApi, type EmailConfiguration, type EmailConfigFormData } from '../lib/emailConfigApi';

const { Option } = Select;

export default function EmailConfigPage() {
    const { t } = useTranslation();
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingConfig, setEditingConfig] = useState<EmailConfiguration | null>(null);
    const [form] = Form.useForm<EmailConfigFormData>();
    const queryClient = useQueryClient();

    // Fetch email configurations
    const { data: configurations, isLoading } = useQuery<EmailConfiguration[]>({
        queryKey: ['emailConfigurations'],
        queryFn: () => emailConfigApi.getAll(),
    });

    // Create/Update mutation
    const saveMutation = useMutation({
        mutationFn: (data: EmailConfigFormData) =>
            editingConfig
                ? emailConfigApi.update(editingConfig.id, data)
                : emailConfigApi.create(data),
        onSuccess: () => {
            message.success(editingConfig ? t('emailConfig.updateSuccess') : t('emailConfig.createSuccess'));
            queryClient.invalidateQueries({ queryKey: ['emailConfigurations'] });
            handleCloseModal();
        },
        onError: (error: Error) => {
            message.error(error.message);
        },
    });

    // Delete mutation
    const deleteMutation = useMutation({
        mutationFn: (id: number) => emailConfigApi.delete(id),
        onSuccess: () => {
            message.success(t('emailConfig.deleteSuccessMessage'));
            queryClient.invalidateQueries({ queryKey: ['emailConfigurations'] });
        },
        onError: (error: Error) => {
            message.error(error.message);
        },
    });

    const handleCloseModal = () => {
        setIsModalOpen(false);
        setEditingConfig(null);
        form.resetFields();
    };

    const handleEdit = (config: EmailConfiguration) => {
        setEditingConfig(config);
        form.setFieldsValue({
            tenantId: config.tenantId,
            provider: config.provider,
            fromEmail: config.fromEmail,
            fromName: config.fromName,
            smtpHost: config.smtpHost,
            smtpPort: config.smtpPort,
            smtpUseSsl: config.smtpUseSsl,
            smtpUsername: config.smtpUsername,
            isEnabled: config.isEnabled,
        });
        setIsModalOpen(true);
    };

    const handleCreate = () => {
        form.setFieldsValue({
            provider: 'None',
            fromEmail: 'noreply@oneid.local',
            fromName: 'OneID',
            smtpPort: 587,
            smtpUseSsl: true,
            isEnabled: true,
        });
        setIsModalOpen(true);
    };

    const handleSubmit = async () => {
        try {
            const values = await form.validateFields();
            saveMutation.mutate(values);
        } catch (error) {
            console.error('Validation failed:', error);
        }
    };

    const provider = Form.useWatch('provider', form);

    const columns: ColumnsType<EmailConfiguration> = [
        {
            title: t('emailConfig.provider'),
            dataIndex: 'provider',
            key: 'provider',
            render: (provider: string) => (
                <Tag color={provider === 'None' ? 'default' : provider === 'Smtp' ? 'blue' : 'green'}>
                    {provider}
                </Tag>
            ),
        },
        {
            title: t('emailConfig.fromEmail'),
            dataIndex: 'fromEmail',
            key: 'fromEmail',
        },
        {
            title: t('emailConfig.fromName'),
            dataIndex: 'fromName',
            key: 'fromName',
        },
        {
            title: t('emailConfig.status'),
            dataIndex: 'isEnabled',
            key: 'isEnabled',
            render: (isEnabled: boolean) => (
                <Tag color={isEnabled ? 'success' : 'default'} icon={isEnabled ? <CheckCircleOutlined /> : undefined}>
                    {isEnabled ? t('emailConfig.enabled') : t('emailConfig.disabled')}
                </Tag>
            ),
        },
        {
            title: t('emailConfig.created'),
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date: string) => new Date(date).toLocaleString(),
        },
        {
            title: t('emailConfig.actions'),
            key: 'actions',
            render: (_, record) => (
                <Space>
                    <Button
                        type="link"
                        icon={<EditOutlined />}
                        onClick={() => handleEdit(record)}
                    >
                        {t('emailConfig.edit')}
                    </Button>
                    <Popconfirm
                        title={t('emailConfig.deleteEmailConfiguration')}
                        description={t('emailConfig.deleteConfirmation')}
                        onConfirm={() => deleteMutation.mutate(record.id)}
                        okText={t('emailConfig.yes')}
                        cancelText={t('emailConfig.no')}
                    >
                        <Button type="link" danger icon={<DeleteOutlined />}>
                            {t('emailConfig.delete')}
                        </Button>
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    return (
        <div style={{ padding: '24px' }}>
            <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h1 style={{ margin: 0 }}>{t('emailConfig.title')}</h1>
                <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
                    {t('emailConfig.addConfiguration')}
                </Button>
            </div>

            <Table
                columns={columns}
                dataSource={configurations || []}
                rowKey="id"
                loading={isLoading}
            />

            <Modal
                title={editingConfig ? t('emailConfig.editEmailConfiguration') : t('emailConfig.createEmailConfiguration')}
                open={isModalOpen}
                onOk={handleSubmit}
                onCancel={handleCloseModal}
                confirmLoading={saveMutation.isPending}
                width={700}
            >
                <Form form={form} layout="vertical">
                    <Form.Item label={t('emailConfig.tenantId')} name="tenantId">
                        <Input placeholder={t('emailConfig.tenantIdPlaceholder')} />
                    </Form.Item>

                    <Form.Item
                        label={t('emailConfig.provider')}
                        name="provider"
                        rules={[{ required: true, message: t('emailConfig.pleaseSelectProvider') }]}
                    >
                        <Select>
                            <Option value="None">{t('emailConfig.providers.none')}</Option>
                            <Option value="Smtp">{t('emailConfig.providers.smtp')}</Option>
                            <Option value="SendGrid">{t('emailConfig.providers.sendgrid')}</Option>
                        </Select>
                    </Form.Item>

                    <Form.Item
                        label={t('emailConfig.fromEmail')}
                        name="fromEmail"
                        rules={[
                            { required: true, message: t('emailConfig.pleaseInputFromEmail') },
                            { type: 'email', message: t('emailConfig.pleaseInputValidEmail') }
                        ]}
                    >
                        <Input placeholder="noreply@oneid.local" />
                    </Form.Item>

                    <Form.Item
                        label={t('emailConfig.fromName')}
                        name="fromName"
                        rules={[{ required: true, message: t('emailConfig.pleaseInputFromName') }]}
                    >
                        <Input placeholder="OneID" />
                    </Form.Item>

                    {provider === 'Smtp' && (
                        <>
                            <Form.Item
                                label={t('emailConfig.smtpHost')}
                                name="smtpHost"
                                rules={[{ required: true, message: t('emailConfig.pleaseInputSmtpHost') }]}
                            >
                                <Input placeholder="smtp.example.com" />
                            </Form.Item>

                            <Form.Item
                                label={t('emailConfig.smtpPort')}
                                name="smtpPort"
                                rules={[{ required: true, message: t('emailConfig.pleaseInputSmtpPort') }]}
                            >
                                <InputNumber min={1} max={65535} placeholder="587" style={{ width: '100%' }} />
                            </Form.Item>

                            <Form.Item label={t('emailConfig.useSslTls')} name="smtpUseSsl" valuePropName="checked">
                                <Switch />
                            </Form.Item>

                            <Form.Item label={t('emailConfig.smtpUsername')} name="smtpUsername">
                                <Input placeholder="username@example.com" />
                            </Form.Item>

                            <Form.Item label={t('emailConfig.smtpPassword')} name="smtpPassword">
                                <Input.Password placeholder={t('emailConfig.keepExistingPassword')} />
                            </Form.Item>
                        </>
                    )}

                    {provider === 'SendGrid' && (
                        <Form.Item label={t('emailConfig.sendGridApiKey')} name="sendGridApiKey">
                            <Input.Password placeholder="SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" />
                        </Form.Item>
                    )}

                    <Form.Item label={t('common.enabled')} name="isEnabled" valuePropName="checked">
                        <Switch />
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    );
}


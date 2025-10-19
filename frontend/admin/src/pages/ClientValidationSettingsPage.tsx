import { useMemo, useEffect } from "react";
import { Card, Form, Input, Select, Switch, Button, Space, Typography, message } from "antd";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { clientSettingsApi } from "../lib/clientSettingsApi";

const SCHEME_OPTIONS = ["https", "http"];

export function ClientValidationSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();

  const { data, isFetching } = useQuery({
    queryKey: ["client-validation-settings"],
    queryFn: () => clientSettingsApi.getValidation(),
  });

  // Update form when data changes
  useEffect(() => {
    if (data) {
      form.setFieldsValue({
        allowedSchemes: data.allowedSchemes,
        allowHttpOnLoopback: data.allowHttpOnLoopback,
        allowedHosts: data.allowedHosts,
      });
    }
  }, [data, form]);

  const mutation = useMutation({
    mutationFn: clientSettingsApi.updateValidation,
    onSuccess: async () => {
      message.success(t("clientValidation.updateSuccess"));
      await queryClient.invalidateQueries({ queryKey: ["client-validation-settings"] });
    },
    onError: (error: any) => {
      const detail = error?.response?.data;
      message.error(detail?.title || detail?.detail || t("clientValidation.updateFailed"));
    },
  });

  const lastUpdated = useMemo(() => {
    if (!data || !('updatedAt' in data) || !(data as any).updatedAt) return "";
    return new Date((data as any).updatedAt).toLocaleString();
  }, [data]);

  const handleSubmit = (values: any) => {
    const payload = {
      allowedSchemes: (values.allowedSchemes || []).map((s: string) => s.trim()).filter(Boolean),
      allowHttpOnLoopback: !!values.allowHttpOnLoopback,
      allowedHosts: (values.allowedHosts || []).map((h: string) => h.trim()).filter(Boolean),
    };

    mutation.mutate(payload);
  };

  return (
    <Card
      title={t("clientValidation.title")}
      extra={lastUpdated ? <Typography.Text type="secondary">{t("clientValidation.lastUpdated")}：{lastUpdated}</Typography.Text> : undefined}
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          allowedSchemes: SCHEME_OPTIONS,
          allowHttpOnLoopback: true,
          allowedHosts: [],
        }}
      >
        <Form.Item
          name="allowedSchemes"
          label={t("clientValidation.allowedSchemes")}
          rules={[{ required: true, message: t("clientValidation.allowedSchemesRequired") }]}
        >
          <Select
            mode="tags"
            options={SCHEME_OPTIONS.map((v) => ({ label: v, value: v }))}
            placeholder={t("clientValidation.allowedSchemesPlaceholder")}
            tokenSeparators={[",", " "]}
          />
        </Form.Item>

        <Form.Item
          name="allowHttpOnLoopback"
          label={t("clientValidation.allowHttpOnLoopback")}
          valuePropName="checked"
        >
          <Switch />
        </Form.Item>

        <Form.Item
          name="allowedHosts"
          label={t("clientValidation.allowedHosts")}
          tooltip={t("clientValidation.allowedHostsTooltip")}
        >
          <Select
            mode="tags"
            tokenSeparators={[",", " "]}
            placeholder={t("clientValidation.allowedHostsPlaceholder")}
          />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={mutation.isPending}>
            {t("clientValidation.saveSettings")}
          </Button>
          <Button onClick={() => form.resetFields()} disabled={isFetching || mutation.isPending}>
            {t("common.reset")}
          </Button>
        </Space>
      </Form>
    </Card>
  );
}

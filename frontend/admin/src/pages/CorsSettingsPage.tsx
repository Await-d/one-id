import { useMemo, useEffect } from "react";
import { Card, Form, Select, Switch, Button, Typography, Space, message } from "antd";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { corsSettingsApi } from "../lib/corsSettingsApi";

export function CorsSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const allowAnyOrigin = Form.useWatch("allowAnyOrigin", form);

  const { data, isFetching } = useQuery({
    queryKey: ["cors-settings"],
    queryFn: () => corsSettingsApi.get(),
  });

  // Update form when data changes
  useEffect(() => {
    if (data) {
      form.setFieldsValue({
        allowedOrigins: data.allowedOrigins,
        allowAnyOrigin: data.allowAnyOrigin,
      });
    }
  }, [data, form]);

  const mutation = useMutation({
    mutationFn: corsSettingsApi.update,
    onSuccess: async () => {
      message.success(t("cors.updateSuccess"));
      await queryClient.invalidateQueries({ queryKey: ["cors-settings"] });
    },
    onError: (error: any) => {
      const detail = error?.response?.data;
      message.error(detail?.title || detail?.detail || t("cors.updateFailed"));
    },
  });

  const lastUpdated = useMemo(() => {
    if (!data || !('updatedAt' in data) || !(data as any).updatedAt) return "";
    return new Date((data as any).updatedAt).toLocaleString();
  }, [data]);

  const handleSubmit = (values: any) => {
    const allowedOrigins = (values.allowedOrigins || [])
      .map((origin: string) => origin.trim())
      .filter(Boolean);

    mutation.mutate({
      allowedOrigins,
      allowAnyOrigin: !!values.allowAnyOrigin,
    });
  };

  return (
    <Card
      title={t("cors.title")}
      extra={lastUpdated ? <Typography.Text type="secondary">{t("cors.lastUpdated")}：{lastUpdated}</Typography.Text> : null}
    >
      <Typography.Paragraph type="secondary">
        {t("cors.description")}
      </Typography.Paragraph>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{ allowAnyOrigin: false, allowedOrigins: [] }}
      >
        <Form.Item name="allowAnyOrigin" label={t("cors.allowAnyOrigin")} valuePropName="checked">
          <Switch />
        </Form.Item>

        <Form.Item name="allowedOrigins" label={t("cors.allowedOrigins")}>
          <Select
            mode="tags"
            tokenSeparators={[",", " "]}
            placeholder={t("cors.originsPlaceholder")}
            disabled={allowAnyOrigin}
          />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={mutation.isPending}>
            {t("cors.saveSettings")}
          </Button>
          <Button onClick={() => form.resetFields()} disabled={isFetching || mutation.isPending}>
            {t("common.reset")}
          </Button>
        </Space>
      </Form>
    </Card>
  );
}

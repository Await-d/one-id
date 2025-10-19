import { Modal, Tabs, Button, message, Typography } from "antd";
import { CopyOutlined, CheckOutlined } from "@ant-design/icons";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { ClientSummary } from "../types/clients";

const { Paragraph, Text } = Typography;

interface IntegrationCodeModalProps {
    client: ClientSummary | null;
    open: boolean;
    onClose: () => void;
}

// 扩展ClientSummary以包含可选的clientSecret（仅在创建时有）
type ClientWithSecret = ClientSummary & { clientSecret?: string };

export function IntegrationCodeModal({ client, open, onClose }: IntegrationCodeModalProps) {
    const { t } = useTranslation();
    const [copiedCode, setCopiedCode] = useState<string | null>(null);

    if (!client) return null;

    const clientWithSecret = client as ClientWithSecret;
    const authority = window.location.origin;
    const redirectUri = client.redirectUris[0] || "https://your-app.com/callback";
    const postLogoutUri = client.postLogoutRedirectUris[0] || "https://your-app.com";
    const scopes = client.scopes.length ? client.scopes.join(" ") : "openid profile email";

    const handleCopy = async (code: string, label: string) => {
        try {
            await navigator.clipboard.writeText(code);
            setCopiedCode(label);
            message.success(t("integration.copySuccess"));
            setTimeout(() => setCopiedCode(null), 2000);
        } catch (error) {
            message.error(t("integration.copyFailed"));
        }
    };

    const CodeBlock = ({ code, language, label }: { code: string; language: string; label: string }) => (
        <div style={{ position: "relative", marginBottom: "16px" }}>
            <Button
                size="small"
                icon={copiedCode === label ? <CheckOutlined /> : <CopyOutlined />}
                onClick={() => handleCopy(code, label)}
                style={{
                    position: "absolute",
                    right: "8px",
                    top: "8px",
                    zIndex: 1,
                }}
            >
                {copiedCode === label ? t("integration.copied") : t("integration.copy")}
            </Button>
            <pre
                style={{
                    background: "#1e1e1e",
                    color: "#d4d4d4",
                    padding: "16px",
                    borderRadius: "8px",
                    overflow: "auto",
                    fontSize: "13px",
                    lineHeight: "1.6",
                    margin: 0,
                }}
            >
                <code className={`language-${language}`}>{code}</code>
            </pre>
        </div>
    );

    // React SPA 示例
    const reactCode = `// 1. ${t("integration.installDependencies")}
npm install oidc-client-ts

// 2. ${t("integration.createAuthService")}
// src/services/authService.ts
import { UserManager, WebStorageStateStore } from "oidc-client-ts";

const config = {
  authority: "${authority}",
  client_id: "${client.clientId}",${client.clientType === "confidential" ? `\n  client_secret: "${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}",` : ""}
  redirect_uri: "${redirectUri}",
  post_logout_redirect_uri: "${postLogoutUri}",
  response_type: "code",
  scope: "${scopes}",
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage }),
};

export const userManager = new UserManager(config);

// 3. ${t("integration.loginLogout")}
export async function login() {
  await userManager.signinRedirect();
}

export async function handleCallback() {
  const user = await userManager.signinRedirectCallback();
  return user;
}

export async function logout() {
  await userManager.signoutRedirect();
}

export async function getUser() {
  return await userManager.getUser();
}

// 4. ${t("integration.useInComponent")}
// src/App.tsx
import { useEffect, useState } from "react";
import { userManager, login, logout, getUser } from "./services/authService";

function App() {
  const [user, setUser] = useState(null);

  useEffect(() => {
    getUser().then(setUser);
  }, []);

  return (
    <div>
      {user ? (
        <div>
          <p>${t("integration.welcome")}: {user.profile.name}</p>
          <button onClick={logout}>${t("integration.logout")}</button>
        </div>
      ) : (
        <button onClick={login}>${t("integration.login")}</button>
      )}
    </div>
  );
}`;

    // Vue 3 示例
    const vueCode = `// 1. ${t("integration.installDependencies")}
npm install oidc-client-ts

// 2. ${t("integration.createAuthService")}
// src/services/auth.ts
import { UserManager } from "oidc-client-ts";

const userManager = new UserManager({
  authority: "${authority}",
  client_id: "${client.clientId}",${client.clientType === "confidential" ? `\n  client_secret: "${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}",` : ""}
  redirect_uri: "${redirectUri}",
  post_logout_redirect_uri: "${postLogoutUri}",
  response_type: "code",
  scope: "${scopes}",
  automaticSilentRenew: true,
});

export const login = () => userManager.signinRedirect();
export const handleCallback = () => userManager.signinRedirectCallback();
export const logout = () => userManager.signoutRedirect();
export const getUser = () => userManager.getUser();

// 3. ${t("integration.useInComponent")}
// src/App.vue
<template>
  <div>
    <div v-if="user">
      <p>${t("integration.welcome")}: {{ user.profile.name }}</p>
      <button @click="handleLogout">${t("integration.logout")}</button>
    </div>
    <button v-else @click="handleLogin">${t("integration.login")}</button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { login, logout, getUser } from "./services/auth";

const user = ref(null);

const handleLogin = () => login();
const handleLogout = () => logout();

onMounted(async () => {
  user.value = await getUser();
});
</script>`;

    // Node.js Backend 示例
    const nodeCode = `// 1. ${t("integration.installDependencies")}
npm install express express-openid-connect

// 2. ${t("integration.configureAuth")}
// server.js
const express = require("express");
const { auth } = require("express-openid-connect");

const app = express();

app.use(
  auth({
    authRequired: false,
    auth0Logout: true,
    issuerBaseURL: "${authority}",
    baseURL: "${redirectUri.replace("/callback", "")}",
    clientID: "${client.clientId}",${client.clientType === "confidential" ? `\n    clientSecret: "${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}",` : ""}
    secret: "LONG_RANDOM_SECRET_STRING",
    idpLogout: true,
    authorizationParams: {
      response_type: "code",
      scope: "${scopes}",
    },
  })
);

// 3. ${t("integration.protectedRoute")}
app.get("/profile", (req, res) => {
  if (!req.oidc.isAuthenticated()) {
    return res.redirect("/login");
  }
  res.json(req.oidc.user);
});

app.listen(3000, () => {
  console.log("Server running on http://localhost:3000");
});`;

    // ASP.NET Core 示例
    const dotnetCode = `// 1. ${t("integration.installDependencies")}
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect

// 2. ${t("integration.configureAuth")}
// Program.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = "${authority}";
    options.ClientId = "${client.clientId}";${client.clientType === "confidential" ? `\n    options.ClientSecret = "${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}";` : ""}
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Clear();
    foreach (var scope in "${scopes}".Split(' '))
    {
        options.Scope.Add(scope);
    }
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 3. ${t("integration.protectedRoute")}
app.MapGet("/profile", (HttpContext context) =>
{
    if (!context.User.Identity?.IsAuthenticated ?? false)
        return Results.Redirect("/login");
    
    return Results.Json(context.User.Claims.Select(c => new { c.Type, c.Value }));
}).RequireAuthorization();

app.Run();`;

    // Java Spring Boot 示例
    const javaCode = `// 1. ${t("integration.installDependencies")}
// pom.xml
<dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-oauth2-client</artifactId>
</dependency>

// 2. ${t("integration.configureAuth")}
// application.yml
spring:
  security:
    oauth2:
      client:
        registration:
          oneid:
            client-id: ${client.clientId}${client.clientType === "confidential" ? `\n            client-secret: ${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}` : ""}
            scope: ${scopes.split(" ").join(", ")}
            redirect-uri: ${redirectUri}
            authorization-grant-type: authorization_code
        provider:
          oneid:
            issuer-uri: ${authority}

// 3. ${t("integration.securityConfig")}
// SecurityConfig.java
@Configuration
@EnableWebSecurity
public class SecurityConfig {
    
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
            .authorizeHttpRequests(authorize -> authorize
                .requestMatchers("/", "/login**").permitAll()
                .anyRequest().authenticated()
            )
            .oauth2Login(Customizer.withDefaults())
            .logout(logout -> logout
                .logoutSuccessUrl("${postLogoutUri}")
            );
        return http.build();
    }
}

// 4. ${t("integration.protectedRoute")}
// ProfileController.java
@RestController
public class ProfileController {
    
    @GetMapping("/profile")
    public Map<String, Object> profile(@AuthenticationPrincipal OAuth2User user) {
        return user.getAttributes();
    }
}`;

    // Python Flask 示例
    const pythonCode = `# 1. ${t("integration.installDependencies")}
pip install Flask Authlib requests

# 2. ${t("integration.configureAuth")}
# app.py
from flask import Flask, redirect, url_for, session
from authlib.integrations.flask_client import OAuth

app = Flask(__name__)
app.secret_key = "RANDOM_SECRET_KEY"

oauth = OAuth(app)
oauth.register(
    name="oneid",
    client_id="${client.clientId}",${client.clientType === "confidential" ? `\n    client_secret="${clientWithSecret.clientSecret || "YOUR_CLIENT_SECRET"}",` : ""}
    server_metadata_url="${authority}/.well-known/openid-configuration",
    client_kwargs={
        "scope": "${scopes}",
    },
)

# 3. ${t("integration.loginLogout")}
@app.route("/login")
def login():
    redirect_uri = url_for("callback", _external=True)
    return oauth.oneid.authorize_redirect(redirect_uri)

@app.route("/callback")
def callback():
    token = oauth.oneid.authorize_access_token()
    user = oauth.oneid.parse_id_token(token)
    session["user"] = user
    return redirect("/profile")

@app.route("/logout")
def logout():
    session.pop("user", None)
    return redirect("${postLogoutUri}")

# 4. ${t("integration.protectedRoute")}
@app.route("/profile")
def profile():
    user = session.get("user")
    if not user:
        return redirect("/login")
    return user

if __name__ == "__main__":
    app.run(debug=True)`;

    const items = [
        { key: "react", label: "React (SPA)", children: <CodeBlock code={reactCode} language="typescript" label="react" /> },
        { key: "vue", label: "Vue 3", children: <CodeBlock code={vueCode} language="typescript" label="vue" /> },
        { key: "nodejs", label: "Node.js (Express)", children: <CodeBlock code={nodeCode} language="javascript" label="nodejs" /> },
        { key: "dotnet", label: "ASP.NET Core", children: <CodeBlock code={dotnetCode} language="csharp" label="dotnet" /> },
        { key: "java", label: "Java (Spring Boot)", children: <CodeBlock code={javaCode} language="java" label="java" /> },
        { key: "python", label: "Python (Flask)", children: <CodeBlock code={pythonCode} language="python" label="python" /> },
    ];

    return (
        <Modal
            title={
                <div>
                    <div style={{ fontSize: "18px", fontWeight: 600 }}>
                        {t("integration.title")}
                    </div>
                    <Text type="secondary" style={{ fontSize: "13px" }}>
                        {t("integration.subtitle", { clientId: client.clientId })}
                    </Text>
                </div>
            }
            open={open}
            onCancel={onClose}
            footer={null}
            width={900}
            styles={{
                body: { maxHeight: "70vh", overflowY: "auto" },
            }}
        >
            <div style={{ marginBottom: "16px" }}>
                <Paragraph>
                    <Text strong>{t("integration.configInfo")}:</Text>
                </Paragraph>
                <div style={{ background: "#f6f8fa", padding: "12px", borderRadius: "6px", fontSize: "13px" }}>
                    <div><Text strong>{t("integration.authority")}:</Text> <Text code>{authority}</Text></div>
                    <div><Text strong>{t("integration.clientId")}:</Text> <Text code>{client.clientId}</Text></div>
                    {client.clientType === "confidential" && (
                        <div><Text strong>{t("integration.clientSecret")}:</Text> <Text code>{clientWithSecret.clientSecret || "********"}</Text></div>
                    )}
                    <div><Text strong>{t("integration.redirectUri")}:</Text> <Text code>{redirectUri}</Text></div>
                    <div><Text strong>{t("integration.scopes")}:</Text> <Text code>{scopes}</Text></div>
                </div>
            </div>

            <Tabs
                defaultActiveKey="react"
                items={items}
                style={{ marginTop: "16px" }}
            />
        </Modal>
    );
}


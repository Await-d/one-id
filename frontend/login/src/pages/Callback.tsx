import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { userManager } from "../lib/oidcClient";
import { Loading } from "../components/Loading";

export function CallbackPage() {
  const navigate = useNavigate();

  useEffect(() => {
    let cancelled = false;

    userManager
      .signinCallback()
      .then(() => {
        if (!cancelled) {
          navigate("/", { replace: true });
        }
      })
      .catch((error) => {
        console.error("Signin callback failed", error);
        if (!cancelled) {
          navigate("/", { replace: true });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [navigate]);

  return <Loading />;
}

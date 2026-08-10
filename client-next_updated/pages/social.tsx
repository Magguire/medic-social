import { useRouter } from 'next/router';
import { useEffect } from 'react';

export default function SocialRedirectPage() {
  const router = useRouter();

  useEffect(() => {
    router.replace('/feed');
  }, [router]);

  return null;
}

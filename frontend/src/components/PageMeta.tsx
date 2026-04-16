import { Helmet } from 'react-helmet-async';
import { useLocation } from 'react-router-dom';

const SITE_NAME = 'JobRecon';
const BASE_URL = 'https://jobrecon.se';
const DEFAULT_DESCRIPTION = 'AI-driven jobbsökning som matchar dina färdigheter med rätt jobb. Hitta ditt perfekta jobb med smart matchning.';
const OG_IMAGE = `${BASE_URL}/images/logo.png`;

interface PageMetaProps {
  title: string;
  description?: string;
  noIndex?: boolean;
}

export function PageMeta({ title, description = DEFAULT_DESCRIPTION, noIndex = false }: PageMetaProps) {
  const { pathname } = useLocation();
  const fullTitle = title === SITE_NAME ? `${SITE_NAME} - AI-driven jobbmatchning` : `${title} | ${SITE_NAME}`;
  const canonicalUrl = `${BASE_URL}${pathname}`;

  return (
    <Helmet>
      <title>{fullTitle}</title>
      <meta name="description" content={description} />
      <link rel="canonical" href={canonicalUrl} />

      {noIndex && <meta name="robots" content="noindex, nofollow" />}

      {/* Open Graph */}
      <meta property="og:type" content="website" />
      <meta property="og:site_name" content={SITE_NAME} />
      <meta property="og:title" content={fullTitle} />
      <meta property="og:description" content={description} />
      <meta property="og:url" content={canonicalUrl} />
      <meta property="og:image" content={OG_IMAGE} />
      <meta property="og:locale" content="sv_SE" />

      {/* Twitter Card */}
      <meta name="twitter:card" content="summary" />
      <meta name="twitter:title" content={fullTitle} />
      <meta name="twitter:description" content={description} />
      <meta name="twitter:image" content={OG_IMAGE} />
    </Helmet>
  );
}

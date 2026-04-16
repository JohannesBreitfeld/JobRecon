import { Container, Typography, Box, Button, Paper, Stack, alpha, Chip } from '@mui/material';
import { Helmet } from 'react-helmet-async';
import { Link as RouterLink } from 'react-router-dom';
import { PageMeta } from '../components/PageMeta';
import {
  SmartToy as SmartToyIcon,
  Notifications as NotificationsIcon,
  Insights as InsightsIcon,
  PersonAdd as PersonAddIcon,
  AutoAwesome as AutoAwesomeIcon,
  WorkOutline as WorkOutlineIcon,
  TrendingUp as TrendingUpIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Lock as LockIcon,
} from '@mui/icons-material';
import { useAuthStore } from '../stores/authStore';
import { useRecommendations } from '../api/hooks/useMatching';

const features = [
  {
    icon: SmartToyIcon,
    title: 'Smart matchning',
    description: 'Vår AI analyserar din profil och hittar jobb som verkligen matchar dina färdigheter och erfarenhet.',
    color: 'secondary.main' as const,
  },
  {
    icon: NotificationsIcon,
    title: 'Realtidsuppdateringar',
    description: 'Få meddelanden direkt när nya jobb som matchar din profil publiceras.',
    color: 'primary.main' as const,
  },
  {
    icon: InsightsIcon,
    title: 'Karriärinsikter',
    description: 'Förstå ditt marknadsvärde och upptäck möjligheter för tillväxt.',
    color: 'primary.dark' as const,
  },
];

const steps = [
  { icon: PersonAddIcon, label: 'Skapa profil', description: 'Berätta om dina färdigheter och erfarenhet' },
  { icon: AutoAwesomeIcon, label: 'AI matchar', description: 'Vår AI hittar de bästa jobben åt dig' },
  { icon: WorkOutlineIcon, label: 'Hitta jobb', description: 'Bläddra bland dina toppträffar' },
];

function AuthenticatedDashboard({ userName }: { userName: string }) {
  const { data: results } = useRecommendations({ pageSize: 3, minScore: 0.5 });

  return (
    <Box
      sx={{
        background: (theme) =>
          `linear-gradient(160deg, ${alpha(theme.palette.primary.dark, 0.04)} 0%, ${alpha(theme.palette.secondary.main, 0.04)} 100%)`,
        pt: { xs: 4, md: 6 },
        pb: { xs: 5, md: 7 },
      }}
    >
      <Container maxWidth="lg">
        <Typography variant="h4" sx={{ mb: 0.5 }}>
          Välkommen tillbaka, {userName}!
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          Här är en snabb översikt av dina matchningar.
        </Typography>

        {/* Quick Stats */}
        {results?.summary && (
          <Paper elevation={0} sx={{ p: 3, mb: 3, border: 1, borderColor: 'divider' }}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
                gap: 3,
              }}
            >
              <Box>
                <Typography variant="body2" color="text.secondary">Matchande jobb</Typography>
                <Typography variant="h5" color="secondary.main">{results.summary.matchedJobs}</Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">Snittscore</Typography>
                <Typography variant="h5">{Math.round(results.summary.averageScore * 100)}%</Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">Analyserade jobb</Typography>
                <Typography variant="h5">{results.summary.totalJobsAnalyzed}</Typography>
              </Box>
              {results.summary.topMatchingSkills.length > 0 && (
                <Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>Toppkompetenser</Typography>
                  <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                    {results.summary.topMatchingSkills.slice(0, 3).map((skill) => (
                      <Chip key={skill} label={skill} size="small" color="secondary" variant="outlined" />
                    ))}
                  </Box>
                </Box>
              )}
            </Box>
          </Paper>
        )}

        {/* Top Matches Preview */}
        {results && results.recommendations.length > 0 && (
          <Box sx={{ mb: 3 }}>
            <Typography variant="h6" sx={{ mb: 1.5 }}>Dina toppträffar</Typography>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              {results.recommendations.slice(0, 3).map((rec) => (
                <Paper
                  key={rec.jobId}
                  elevation={0}
                  sx={{
                    p: 2.5,
                    flex: 1,
                    border: 1,
                    borderColor: 'divider',
                    borderLeft: 3,
                    borderLeftColor: rec.matchScore >= 0.8 ? '#5ba532' : rec.matchScore >= 0.6 ? '#1565a0' : '#e88a1a',
                  }}
                >
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <Box sx={{ flex: 1, minWidth: 0 }}>
                      <Typography variant="subtitle2" noWrap>{rec.title}</Typography>
                      <Typography variant="caption" color="text.secondary">{rec.companyName}</Typography>
                      {rec.location && (
                        <Typography variant="caption" color="text.secondary" display="block">{rec.location}</Typography>
                      )}
                    </Box>
                    <Box
                      sx={{
                        ml: 1.5,
                        width: 40,
                        height: 40,
                        borderRadius: '50%',
                        bgcolor: rec.matchScore >= 0.8 ? '#5ba532' : rec.matchScore >= 0.6 ? '#1565a0' : '#e88a1a',
                        color: '#fff',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '0.8rem',
                        fontWeight: 700,
                        flexShrink: 0,
                      }}
                    >
                      {Math.round(rec.matchScore * 100)}%
                    </Box>
                  </Box>
                </Paper>
              ))}
            </Stack>
          </Box>
        )}

        {/* Quick Actions */}
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <Button
            variant="contained"
            color="secondary"
            component={RouterLink}
            to="/recommendations"
            startIcon={<TrendingUpIcon />}
            sx={{ px: 3 }}
          >
            Se rekommendationer
          </Button>
          <Button
            variant="outlined"
            component={RouterLink}
            to="/jobs"
            startIcon={<SearchIcon />}
            sx={{ px: 3 }}
          >
            Sök jobb
          </Button>
          <Button
            variant="outlined"
            component={RouterLink}
            to="/profile"
            startIcon={<EditIcon />}
            sx={{ px: 3 }}
          >
            Uppdatera profil
          </Button>
        </Box>
      </Container>
    </Box>
  );
}

function ProductPreview() {
  return (
    <Box sx={{ py: { xs: 6, md: 8 }, bgcolor: (theme) => alpha(theme.palette.primary.dark, 0.02) }}>
      <Container maxWidth="md">
        <Typography variant="h4" align="center" sx={{ mb: 1 }}>
          Se hur det fungerar
        </Typography>
        <Typography variant="body1" align="center" color="text.secondary" sx={{ mb: 4, maxWidth: 500, mx: 'auto' }}>
          Så ser dina personliga jobbmatchningar ut
        </Typography>

        <Paper elevation={0} sx={{ p: 3, border: 1, borderColor: 'divider', position: 'relative', overflow: 'hidden' }}>
          {/* Fake stats row */}
          <Box sx={{ display: 'flex', gap: 4, mb: 3, flexWrap: 'wrap' }}>
            <Box>
              <Typography variant="caption" color="text.secondary">Matchande jobb</Typography>
              <Typography variant="h6" sx={{ filter: 'blur(3px)', userSelect: 'none' }}>142</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Snittscore</Typography>
              <Typography variant="h6" sx={{ filter: 'blur(3px)', userSelect: 'none' }}>73%</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Toppkompetenser</Typography>
              <Box sx={{ display: 'flex', gap: 0.5, mt: 0.5, filter: 'blur(3px)' }}>
                <Chip label="React" size="small" variant="outlined" />
                <Chip label=".NET" size="small" variant="outlined" />
              </Box>
            </Box>
          </Box>

          {/* Fake match cards */}
          {[85, 72, 68].map((score, i) => (
            <Paper
              key={i}
              variant="outlined"
              sx={{
                p: 2,
                mb: 1.5,
                filter: 'blur(2px)',
                userSelect: 'none',
                borderLeft: 3,
                borderLeftColor: score >= 80 ? '#5ba532' : '#1565a0',
              }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Box>
                  <Typography variant="subtitle2">Senior Fullstackutvecklare</Typography>
                  <Typography variant="caption" color="text.secondary">TechBolag AB - Stockholm</Typography>
                </Box>
                <Box
                  sx={{
                    width: 36,
                    height: 36,
                    borderRadius: '50%',
                    bgcolor: score >= 80 ? '#5ba532' : '#1565a0',
                    color: '#fff',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '0.75rem',
                    fontWeight: 700,
                  }}
                >
                  {score}%
                </Box>
              </Box>
            </Paper>
          ))}

          {/* Overlay CTA */}
          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255,255,255,0.7)',
              backdropFilter: 'blur(2px)',
            }}
          >
            <LockIcon sx={{ fontSize: 40, color: 'primary.main', mb: 1.5 }} />
            <Typography variant="h6" sx={{ mb: 1 }}>
              Dina matchningar väntar
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5, textAlign: 'center', maxWidth: 320 }}>
              Registrera dig för att se vilka jobb som matchar din profil bäst.
            </Typography>
            <Button
              variant="contained"
              color="secondary"
              component={RouterLink}
              to="/register"
              sx={{ px: 4 }}
            >
              Registrera dig gratis
            </Button>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}

export function HomePage() {
  const { isAuthenticated, user } = useAuthStore();

  const jsonLd = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebSite',
        name: 'JobRecon',
        url: 'https://jobrecon.se',
        description: 'AI-driven jobbmatchning som förstår dina färdigheter, erfarenhet och karriärmål.',
        inLanguage: 'sv-SE',
      },
      {
        '@type': 'Organization',
        name: 'JobRecon',
        url: 'https://jobrecon.se',
        logo: 'https://jobrecon.se/images/logo.png',
      },
    ],
  };

  return (
    <Box>
      <PageMeta
        title="JobRecon"
        description="AI-driven jobbmatchning som förstår dina färdigheter, erfarenhet och karriärmål. Hitta ditt perfekta jobb med smart matchning."
      />
      <Helmet>
        <script type="application/ld+json">{JSON.stringify(jsonLd)}</script>
      </Helmet>
      {isAuthenticated ? (
        <AuthenticatedDashboard userName={user?.firstName || user?.email || ''} />
      ) : (
        <>
          {/* Hero Section */}
          <Box
            sx={{
              background: (theme) =>
                `linear-gradient(160deg, ${alpha(theme.palette.primary.dark, 0.04)} 0%, ${alpha(theme.palette.secondary.main, 0.06)} 50%, ${alpha(theme.palette.primary.main, 0.03)} 100%)`,
              pt: { xs: 6, md: 10 },
              pb: { xs: 8, md: 12 },
            }}
          >
            <Container maxWidth="md" sx={{ textAlign: 'center' }}>
              <Box
                component="img"
                src="/images/logo.png"
                alt="JobRecon"
                sx={{
                  width: { xs: 180, md: 240 },
                  height: 'auto',
                  mb: 3,
                }}
              />
              <Typography
                variant="h2"
                component="h1"
                sx={{ fontSize: { xs: '2rem', md: '3rem' }, mb: 2 }}
              >
                Hitta ditt perfekta jobb
              </Typography>
              <Typography
                variant="h6"
                color="text.secondary"
                sx={{ mb: 5, maxWidth: 560, mx: 'auto', fontWeight: 400, lineHeight: 1.6 }}
              >
                AI-driven jobbmatchning som förstår dina färdigheter, erfarenhet och karriärmål
              </Typography>
              <Box>
                <Button
                  variant="contained"
                  color="secondary"
                  size="large"
                  component={RouterLink}
                  to="/register"
                  sx={{ mr: 2, px: 4, py: 1.2 }}
                >
                  Kom igång
                </Button>
                <Button
                  variant="outlined"
                  size="large"
                  component={RouterLink}
                  to="/login"
                  sx={{ px: 4, py: 1.2 }}
                >
                  Logga in
                </Button>
              </Box>
            </Container>
          </Box>
        </>
      )}

      {/* Features Section */}
      <Container maxWidth="lg" sx={{ py: { xs: 6, md: 8 } }}>
        <Typography variant="h4" align="center" sx={{ mb: 1 }}>
          Varför JobRecon?
        </Typography>
        <Typography
          variant="body1"
          align="center"
          color="text.secondary"
          sx={{ mb: 5, maxWidth: 500, mx: 'auto' }}
        >
          Vi kombinerar AI med djup förståelse för arbetsmarknaden
        </Typography>

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
          {features.map((feature) => (
            <Paper
              key={feature.title}
              elevation={0}
              sx={{
                p: 4,
                flex: 1,
                border: 1,
                borderColor: 'divider',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'flex-start',
                transition: 'transform 0.2s ease, box-shadow 0.2s ease',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: '0 12px 32px rgba(0,0,0,0.08)',
                },
              }}
            >
              <Box
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  bgcolor: (theme) => {
                    const [palette, shade] = feature.color.split('.');
                    const paletteObj = theme.palette[palette as keyof typeof theme.palette];
                    const colorValue = typeof paletteObj === 'object' && paletteObj !== null && shade in paletteObj
                      ? (paletteObj as unknown as Record<string, string>)[shade]
                      : theme.palette.secondary.main;
                    return alpha(colorValue, 0.1);
                  },
                  color: feature.color,
                  mb: 2,
                }}
              >
                <feature.icon fontSize="large" />
              </Box>
              <Typography variant="h6" sx={{ mb: 1 }}>
                {feature.title}
              </Typography>
              <Typography color="text.secondary" variant="body2" sx={{ lineHeight: 1.7 }}>
                {feature.description}
              </Typography>
            </Paper>
          ))}
        </Stack>
      </Container>

      {/* How It Works Section */}
      <Box sx={{ bgcolor: 'background.paper', py: { xs: 6, md: 8 } }}>
        <Container maxWidth="md">
          <Typography variant="h4" align="center" sx={{ mb: 1 }}>
            Så fungerar det
          </Typography>
          <Typography
            variant="body1"
            align="center"
            color="text.secondary"
            sx={{ mb: 6, maxWidth: 400, mx: 'auto' }}
          >
            Tre enkla steg till ditt nästa jobb
          </Typography>

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={{ xs: 4, md: 2 }}
            alignItems="flex-start"
          >
            {steps.map((step, index) => (
              <Box key={step.label} sx={{ flex: 1, textAlign: 'center', position: 'relative' }}>
                <Box
                  sx={{
                    width: 64,
                    height: 64,
                    borderRadius: '50%',
                    bgcolor: 'primary.dark',
                    color: 'white',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    mx: 'auto',
                    mb: 2,
                    position: 'relative',
                  }}
                >
                  <step.icon sx={{ fontSize: 28 }} />
                  <Box
                    sx={{
                      position: 'absolute',
                      top: -4,
                      right: -4,
                      width: 24,
                      height: 24,
                      borderRadius: '50%',
                      bgcolor: 'secondary.main',
                      color: 'white',
                      fontSize: '0.75rem',
                      fontWeight: 700,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    {index + 1}
                  </Box>
                </Box>
                <Typography variant="h6" sx={{ mb: 0.5 }}>{step.label}</Typography>
                <Typography variant="body2" color="text.secondary">{step.description}</Typography>
              </Box>
            ))}
          </Stack>
        </Container>
      </Box>

      {/* Product Preview - only for non-auth users */}
      {!isAuthenticated && <ProductPreview />}
    </Box>
  );
}

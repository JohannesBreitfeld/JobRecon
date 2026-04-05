import { useState } from 'react';
import {
  Container,
  Paper,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Button,
  LinearProgress,
  Collapse,
  IconButton,
  alpha,
} from '@mui/material';
import {
  Person as PersonIcon,
  Code as CodeIcon,
  Tune as TuneIcon,
  ExpandMore as ExpandMoreIcon,
} from '@mui/icons-material';
import { useProfile, useCreateProfile } from '../../api/hooks/useProfile';
import { ApiError } from '../../api/client';
import type { ProfileResponse } from '../../api/profile';
import { ProfileForm } from './ProfileForm';
import { SkillsSection } from './SkillsSection';
import { PreferencesSection } from './PreferencesSection';

function calculateCompleteness(profile: ProfileResponse): number {
  let score = 0;
  if (profile.currentJobTitle) score += 15;
  if (profile.summary) score += 15;
  if (profile.location) score += 10;
  if (profile.yearsOfExperience != null) score += 10;
  if (profile.desiredJobTitles.length > 0) score += 15;
  if (profile.skills.length > 0) score += 15;
  if (profile.jobPreference) score += 20;
  return score;
}

function getCompletenessMessage(score: number): string {
  if (score < 50) return 'Fyll i mer för bättre matchningar';
  if (score <= 80) return 'Bra start! Lägg till mer för ännu bättre resultat';
  return 'Utmärkt! Din profil är väl ifylld';
}

function getCompletenessColor(score: number): string {
  if (score < 50) return '#e88a1a';
  if (score <= 80) return '#1565a0';
  return '#5ba532';
}

interface SectionHeaderProps {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
  expanded: boolean;
  onToggle: () => void;
}

function SectionHeader({ icon, title, subtitle, expanded, onToggle }: SectionHeaderProps) {
  return (
    <Box
      sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: expanded ? 2.5 : 0, cursor: 'pointer' }}
      onClick={onToggle}
    >
      <Box
        sx={{
          p: 1,
          borderRadius: 2,
          bgcolor: (theme) => alpha(theme.palette.primary.main, 0.08),
          color: 'primary.main',
          display: 'flex',
        }}
      >
        {icon}
      </Box>
      <Box sx={{ flex: 1 }}>
        <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {subtitle}
        </Typography>
      </Box>
      <IconButton
        size="small"
        aria-label={expanded ? `Dölj ${title}` : `Visa ${title}`}
        sx={{
          transform: expanded ? 'rotate(180deg)' : 'rotate(0deg)',
          transition: 'transform 0.2s',
        }}
      >
        <ExpandMoreIcon />
      </IconButton>
    </Box>
  );
}

export function ProfilePage() {
  const { data: profile, isLoading, error } = useProfile();
  const createProfileMutation = useCreateProfile();
  const [basicExpanded, setBasicExpanded] = useState(true);
  const [skillsExpanded, setSkillsExpanded] = useState(true);
  const [prefsExpanded, setPrefsExpanded] = useState(true);

  const profileNotFound = error instanceof ApiError && error.code === 'Profile.NotFound';

  const handleCreateProfile = () => {
    createProfileMutation.mutate({});
  };

  if (isLoading) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (!profile && profileNotFound) {
    const steps = [
      { label: 'Skapa profil', active: true },
      { label: 'Lägg till kompetenser', active: false },
      { label: 'Se matchningar', active: false },
    ];

    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Paper sx={{ p: 5, textAlign: 'center' }}>
          <Box
            component="img"
            src="/images/logo.png"
            alt="JobRecon"
            sx={{ width: 120, height: 'auto', mb: 3, opacity: 0.8 }}
          />
          <Typography variant="h5" gutterBottom>
            Välkommen till JobRecon!
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3, maxWidth: 400, mx: 'auto' }}>
            Du har inte skapat en profil ännu. Skapa en profil för att börja matcha med jobb.
          </Typography>

          <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mb: 4, flexWrap: 'wrap' }}>
            {steps.map((step, i) => (
              <Box key={step.label} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box
                  sx={{
                    width: 28,
                    height: 28,
                    borderRadius: '50%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: step.active ? 'secondary.main' : 'grey.300',
                    color: step.active ? '#fff' : 'text.secondary',
                    fontSize: '0.8rem',
                    fontWeight: 600,
                  }}
                >
                  {i + 1}
                </Box>
                <Typography
                  variant="body2"
                  sx={{ fontWeight: step.active ? 600 : 400, color: step.active ? 'text.primary' : 'text.secondary' }}
                >
                  {step.label}
                </Typography>
                {i < steps.length - 1 && (
                  <Box sx={{ width: 24, height: 1, bgcolor: 'divider', mx: 0.5 }} />
                )}
              </Box>
            ))}
          </Box>

          <Button
            variant="contained"
            color="secondary"
            size="large"
            onClick={handleCreateProfile}
            disabled={createProfileMutation.isPending}
            sx={{ px: 4 }}
          >
            {createProfileMutation.isPending ? 'Skapar profil...' : 'Skapa min profil'}
          </Button>
        </Paper>
      </Container>
    );
  }

  if (error && !profileNotFound) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Kunde inte hämta profil'}
        </Alert>
      </Container>
    );
  }

  const completeness = profile ? calculateCompleteness(profile) : 0;

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>
        Min profil
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Håll din profil uppdaterad för bästa matchningsresultat.
      </Typography>

      {profile && (
        <Paper elevation={0} sx={{ p: 2, mb: 3, border: 1, borderColor: 'divider' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="subtitle2">
              Profilstatus: {completeness}%
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {getCompletenessMessage(completeness)}
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={completeness}
            sx={{
              height: 8,
              borderRadius: 4,
              backgroundColor: 'grey.200',
              '& .MuiLinearProgress-bar': {
                borderRadius: 4,
                backgroundColor: getCompletenessColor(completeness),
              },
            }}
          />
        </Paper>
      )}

      <Paper elevation={0} sx={{ p: { xs: 2, sm: 3 }, mt: 2, border: 1, borderColor: 'divider' }}>
        <SectionHeader
          icon={<PersonIcon />}
          title="Grundläggande information"
          subtitle="Din erfarenhet och bakgrund"
          expanded={basicExpanded}
          onToggle={() => setBasicExpanded(!basicExpanded)}
        />
        <Collapse in={basicExpanded}>
          <ProfileForm />
        </Collapse>
      </Paper>

      <Paper elevation={0} sx={{ p: { xs: 2, sm: 3 }, mt: 3, border: 1, borderColor: 'divider' }}>
        <SectionHeader
          icon={<TuneIcon />}
          title="Jobbpreferenser"
          subtitle="Vad du letar efter i ditt nästa jobb"
          expanded={prefsExpanded}
          onToggle={() => setPrefsExpanded(!prefsExpanded)}
        />
        <Collapse in={prefsExpanded}>
          <PreferencesSection />
        </Collapse>
      </Paper>

      <Paper elevation={0} sx={{ p: { xs: 2, sm: 3 }, mt: 3, border: 1, borderColor: 'divider' }}>
        <SectionHeader
          icon={<CodeIcon />}
          title="Kompetenser"
          subtitle="Dina tekniska och professionella färdigheter"
          expanded={skillsExpanded}
          onToggle={() => setSkillsExpanded(!skillsExpanded)}
        />
        <Collapse in={skillsExpanded}>
          <SkillsSection />
        </Collapse>
      </Paper>
    </Container>
  );
}

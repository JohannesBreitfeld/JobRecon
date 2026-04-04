import { useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Alert,
  Button,
  CircularProgress,
  Pagination,
  Paper,
  Slider,
  Chip,
  alpha,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  Analytics as AnalyticsIcon,
  WorkOutline as WorkOutlineIcon,
  Star as StarIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useMatchingStore } from '../../stores/matchingStore';
import { useRecommendations } from '../../api/hooks/useMatching';
import { ApiError } from '../../api/client';
import { MatchCard } from './MatchCard';
import { JobDetailsDialog } from '../jobs/JobDetailsDialog';

export function RecommendationsPage() {
  const navigate = useNavigate();
  const { params, setParams } = useMatchingStore();
  const { data: results, isLoading, error } = useRecommendations(params);
  const [minScoreFilter, setMinScoreFilter] = useState(params.minScore ?? 0.6);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);

  const profileNotFound = error instanceof ApiError && (error.code === 'Profile.NotFound' || error.status === 404);

  const handlePageChange = (_: React.ChangeEvent<unknown>, page: number) => {
    setParams({ page });
  };

  const handleMinScoreChange = (_: Event, value: number | number[]) => {
    const score = value as number;
    setMinScoreFilter(score);
  };

  const handleMinScoreCommit = (_: Event | React.SyntheticEvent, value: number | number[]) => {
    const score = value as number;
    setParams({ minScore: score, page: 1 });
  };

  const totalPages = results ? Math.ceil(results.totalCount / results.pageSize) : 0;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
          <TrendingUpIcon color="primary" />
          <Typography variant="h4" component="h1">
            Rekommendationer
          </Typography>
        </Box>
        <Typography variant="body1" color="text.secondary">
          Jobb som matchar din profil, rankade efter relevans.
        </Typography>
      </Box>

      {profileNotFound && (
        <Paper elevation={0} sx={{ p: 5, textAlign: 'center', border: 1, borderColor: 'divider' }}>
          <Box
            component="img"
            src="/images/logo.png"
            alt="JobRecon"
            sx={{ width: 100, height: 'auto', mb: 3, opacity: 0.7 }}
          />
          <Typography variant="h5" gutterBottom>
            Skapa din profil för att se rekommendationer
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3, maxWidth: 400, mx: 'auto' }}>
            Vi behöver veta lite om dig för att kunna matcha dig med rätt jobb.
          </Typography>
          <Button variant="contained" color="secondary" size="large" onClick={() => navigate('/profile')}>
            Gå till profil
          </Button>
        </Paper>
      )}

      {error && !profileNotFound && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error instanceof Error ? error.message : 'Ett fel uppstod vid hämtning av rekommendationer'}
        </Alert>
      )}

      {results?.summary && (
        <Paper elevation={0} sx={{ p: 3, mb: 3, border: 1, borderColor: 'divider' }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
              gap: 3,
            }}
          >
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Box
                sx={{
                  p: 1,
                  borderRadius: 2,
                  bgcolor: (theme) => alpha(theme.palette.primary.main, 0.08),
                  color: 'primary.main',
                  display: 'flex',
                }}
              >
                <AnalyticsIcon fontSize="small" />
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Analyserade jobb
                </Typography>
                <Typography variant="h6">{results.summary.totalJobsAnalyzed}</Typography>
              </Box>
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Box
                sx={{
                  p: 1,
                  borderRadius: 2,
                  bgcolor: (theme) => alpha(theme.palette.secondary.main, 0.08),
                  color: 'secondary.main',
                  display: 'flex',
                }}
              >
                <WorkOutlineIcon fontSize="small" />
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Matchande jobb
                </Typography>
                <Typography variant="h6">{results.summary.matchedJobs}</Typography>
              </Box>
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              <Box
                sx={{
                  p: 1,
                  borderRadius: 2,
                  bgcolor: (theme) => alpha(theme.palette.success.main, 0.08),
                  color: 'success.main',
                  display: 'flex',
                }}
              >
                <StarIcon fontSize="small" />
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Snittscore
                </Typography>
                <Typography variant="h6">
                  {Math.round(results.summary.averageScore * 100)}%
                </Typography>
              </Box>
            </Box>

            {results.summary.topMatchingSkills.length > 0 && (
              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  Dina bästa kompetenser
                </Typography>
                <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                  {results.summary.topMatchingSkills.map((skill) => (
                    <Chip key={skill} label={skill} size="small" color="secondary" variant="outlined" />
                  ))}
                </Box>
              </Box>
            )}
          </Box>

          <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
            Baserat på jobb från senaste 30 dagarna &middot; Uppdaterad {new Date().toLocaleDateString('sv-SE')}
          </Typography>

          <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Minsta matchscore: <strong>{Math.round(minScoreFilter * 100)}%</strong>
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
              Dra reglaget för att filtrera bort jobb med lägre matchningsgrad.
            </Typography>
            <Slider
              value={minScoreFilter}
              onChange={handleMinScoreChange}
              onChangeCommitted={handleMinScoreCommit}
              min={0}
              max={1}
              step={0.05}
              valueLabelDisplay="auto"
              valueLabelFormat={(v) => `${Math.round(v * 100)}%`}
              marks={[
                { value: 0, label: '0%' },
                { value: 0.5, label: '50%' },
                { value: 1, label: '100%' },
              ]}
              sx={{ maxWidth: 400, color: 'secondary.main' }}
            />
          </Box>
        </Paper>
      )}

      {isLoading && !results && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {results && results.recommendations.length === 0 && !isLoading && (
        <Paper elevation={0} sx={{ p: 5, textAlign: 'center', border: 1, borderColor: 'divider' }}>
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Inga rekommendationer hittades
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Prova att sänka minsta matchscore eller uppdatera din profil med fler kompetenser och önskade titlar.
          </Typography>
        </Paper>
      )}

      {results?.recommendations.map((rec) => (
        <MatchCard key={rec.jobId} recommendation={rec} onJobClick={setSelectedJobId} />
      ))}

      <JobDetailsDialog
        jobId={selectedJobId}
        open={selectedJobId !== null}
        onClose={() => setSelectedJobId(null)}
      />

      {totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
          <Pagination
            count={totalPages}
            page={results?.page ?? 1}
            onChange={handlePageChange}
            color="primary"
          />
        </Box>
      )}
    </Container>
  );
}

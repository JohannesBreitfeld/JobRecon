import { useState } from 'react';
import {
  Card,
  CardContent,
  Box,
  Typography,
  Chip,
  IconButton,
  Collapse,
  Link,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  LocationOn as LocationIcon,
  Business as BusinessIcon,
  OpenInNew as OpenInNewIcon,
} from '@mui/icons-material';
import type { JobRecommendation } from '../../api/matching';
import { ScoreBreakdown } from './ScoreBreakdown';

interface MatchCardProps {
  recommendation: JobRecommendation;
}

export function MatchCard({ recommendation }: MatchCardProps) {
  const [expanded, setExpanded] = useState(false);

  const scorePercent = Math.round(recommendation.matchScore * 100);

  const formatSalary = () => {
    if (!recommendation.salaryMin && !recommendation.salaryMax) return null;
    const currency = recommendation.salaryCurrency || 'SEK';
    if (recommendation.salaryMin && recommendation.salaryMax) {
      return `${recommendation.salaryMin.toLocaleString('sv-SE')} - ${recommendation.salaryMax.toLocaleString('sv-SE')} ${currency}`;
    }
    if (recommendation.salaryMin) {
      return `Från ${recommendation.salaryMin.toLocaleString('sv-SE')} ${currency}`;
    }
    return `Upp till ${recommendation.salaryMax!.toLocaleString('sv-SE')} ${currency}`;
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    if (diffDays === 0) return 'Idag';
    if (diffDays === 1) return 'Igår';
    if (diffDays < 7) return `${diffDays} dagar sedan`;
    return date.toLocaleDateString('sv-SE');
  };

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box sx={{ flex: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
              <Typography variant="h6" component="h3">
                {recommendation.title}
              </Typography>
              {recommendation.externalUrl && (
                <Link href={recommendation.externalUrl} target="_blank" rel="noopener">
                  <OpenInNewIcon fontSize="small" color="action" />
                </Link>
              )}
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1, flexWrap: 'wrap' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                <BusinessIcon fontSize="small" color="action" />
                <Typography variant="body2" color="text.secondary">
                  {recommendation.companyName}
                </Typography>
              </Box>
              {recommendation.location && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <LocationIcon fontSize="small" color="action" />
                  <Typography variant="body2" color="text.secondary">
                    {recommendation.location}
                  </Typography>
                </Box>
              )}
              {recommendation.postedAt && (
                <Typography variant="body2" color="text.secondary">
                  {formatDate(recommendation.postedAt)}
                </Typography>
              )}
            </Box>

            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              {recommendation.workLocationType && (
                <Chip label={recommendation.workLocationType} size="small" variant="outlined" />
              )}
              {recommendation.employmentType && (
                <Chip label={recommendation.employmentType} size="small" variant="outlined" />
              )}
              {formatSalary() && (
                <Chip label={formatSalary()} size="small" color="primary" variant="outlined" />
              )}
            </Box>
          </Box>

          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', ml: 2 }}>
            <Box
              sx={{
                width: 56,
                height: 56,
                borderRadius: '50%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: getScoreBgColor(recommendation.matchScore),
                color: '#fff',
                fontWeight: 'bold',
                fontSize: '1.1rem',
              }}
            >
              {scorePercent}%
            </Box>
            <IconButton
              size="small"
              onClick={() => setExpanded(!expanded)}
              sx={{
                mt: 0.5,
                transform: expanded ? 'rotate(180deg)' : 'rotate(0deg)',
                transition: 'transform 0.2s',
              }}
            >
              <ExpandMoreIcon fontSize="small" />
            </IconButton>
          </Box>
        </Box>

        <Collapse in={expanded}>
          <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
            <Typography variant="subtitle2" gutterBottom>
              Matchningsdetaljer
            </Typography>
            <ScoreBreakdown factors={recommendation.matchFactors} />
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
}

function getScoreBgColor(score: number): string {
  if (score >= 0.8) return '#2e7d32';
  if (score >= 0.6) return '#1976d2';
  if (score >= 0.4) return '#ed6c02';
  return '#d32f2f';
}

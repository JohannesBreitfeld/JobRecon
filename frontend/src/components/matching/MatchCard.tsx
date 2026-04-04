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
  Tooltip,
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
  onJobClick: (jobId: string) => void;
}

export function MatchCard({ recommendation, onJobClick }: MatchCardProps) {
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
    <Card
      elevation={0}
      sx={{
        mb: 2,
        border: 1,
        borderColor: 'divider',
        transition: 'box-shadow 0.15s ease',
        '&:hover': { boxShadow: '0 4px 16px rgba(0,0,0,0.06)' },
      }}
    >
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box sx={{ flex: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
              <Typography
                variant="h6"
                component="h3"
                onClick={() => onJobClick(recommendation.jobId)}
                sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
              >
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
            <Tooltip title="Matchscore visar hur väl jobbet passar din profil baserat på kompetenser, erfarenhet och preferenser">
              <Box
                sx={{
                  width: 60,
                  height: 60,
                  borderRadius: '50%',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  backgroundColor: getScoreBgColor(recommendation.matchScore),
                  color: '#fff',
                  fontWeight: 'bold',
                  cursor: 'help',
                }}
              >
                <Box sx={{ fontSize: '1.1rem', lineHeight: 1 }}>{scorePercent}%</Box>
                <Box sx={{ fontSize: '0.55rem', fontWeight: 500, mt: 0.2, opacity: 0.9 }}>
                  {getScoreLabel(recommendation.matchScore)}
                </Box>
              </Box>
            </Tooltip>
            <IconButton
              size="small"
              onClick={() => setExpanded(!expanded)}
              aria-label={expanded ? 'Dölj detaljer' : 'Visa detaljer'}
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
  if (score >= 0.8) return '#5ba532';
  if (score >= 0.6) return '#1565a0';
  if (score >= 0.4) return '#e88a1a';
  return '#d32f2f';
}

function getScoreLabel(score: number): string {
  if (score >= 0.8) return 'UTMÄRKT';
  if (score >= 0.6) return 'BRA';
  if (score >= 0.4) return 'GODKÄND';
  return 'LÅG';
}

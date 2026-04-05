import { useMemo } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Divider,
  CircularProgress,
  Link,
  IconButton,
  FormControl,
  Select,
  MenuItem,
  InputLabel,
} from '@mui/material';
import DOMPurify from 'dompurify';
import {
  Close as CloseIcon,
  Bookmark as BookmarkIcon,
  BookmarkBorder as BookmarkBorderIcon,
  OpenInNew as OpenInNewIcon,
  LocationOn as LocationIcon,
  Work as WorkIcon,
  AttachMoney as MoneyIcon,
  Schedule as ScheduleIcon,
  Business as BusinessIcon,
} from '@mui/icons-material';
import { useJob, useSaveJob, useRemoveSavedJob, useUpdateSavedJob } from '../../api/hooks/useJobs';
import { useProfile } from '../../api/hooks/useProfile';
import { SkillChip } from './SkillChip';
import type { WorkLocationType, EmploymentType, SavedJobStatus } from '../../api/jobs';

const workLocationLabels: Record<WorkLocationType, string> = {
  OnSite: 'På plats',
  Remote: 'Distans',
  Hybrid: 'Hybrid',
};

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Contract: 'Kontrakt',
  Freelance: 'Frilans',
  Internship: 'Praktik',
  Temporary: 'Tidsbegränsad',
};

const savedStatusLabels: Record<SavedJobStatus, string> = {
  Saved: 'Sparad',
  Applied: 'Ansökt',
  Interviewing: 'Intervju',
  Rejected: 'Avvisad',
  Offered: 'Erbjudande',
  Accepted: 'Accepterad',
  Withdrawn: 'Återtagen',
};

interface JobDetailsDialogProps {
  jobId: string | null;
  open: boolean;
  onClose: () => void;
}

export function JobDetailsDialog({ jobId, open, onClose }: JobDetailsDialogProps) {
  const { data: selectedJob, isLoading } = useJob(open ? jobId : null);
  useProfile();

  const saveJobMutation = useSaveJob();
  const removeSavedJobMutation = useRemoveSavedJob();
  const updateSavedJobMutation = useUpdateSavedJob();

  const isMutating = saveJobMutation.isPending || removeSavedJobMutation.isPending || updateSavedJobMutation.isPending;

  const handleSaveClick = () => {
    if (!selectedJob) return;
    if (selectedJob.isSaved) {
      removeSavedJobMutation.mutate(selectedJob.id);
    } else {
      saveJobMutation.mutate({ jobId: selectedJob.id });
    }
  };

  const handleStatusChange = (status: SavedJobStatus) => {
    if (!selectedJob) return;
    updateSavedJobMutation.mutate({ jobId: selectedJob.id, status });
  };

  const formatSalary = () => {
    if (!selectedJob?.salaryMin && !selectedJob?.salaryMax) return null;
    const currency = selectedJob.salaryCurrency || 'SEK';
    const period = selectedJob.salaryPeriod === 'monthly' ? '/mån' : selectedJob.salaryPeriod === 'yearly' ? '/år' : '';
    if (selectedJob.salaryMin && selectedJob.salaryMax) {
      return `${selectedJob.salaryMin.toLocaleString()} - ${selectedJob.salaryMax.toLocaleString()} ${currency}${period}`;
    }
    if (selectedJob.salaryMin) {
      return `Från ${selectedJob.salaryMin.toLocaleString()} ${currency}${period}`;
    }
    return `Upp till ${selectedJob.salaryMax?.toLocaleString()} ${currency}${period}`;
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return null;
    return new Date(dateStr).toLocaleDateString('sv-SE', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        {isLoading ? 'Laddar...' : selectedJob?.title || 'Jobbdetaljer'}
        <IconButton onClick={onClose} size="small" aria-label="Stang">
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        {isLoading && !selectedJob ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : selectedJob ? (
          <Box>
            {/* Header */}
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, mb: 3 }}>
              {selectedJob.company.logoUrl ? (
                <Box
                  component="img"
                  src={selectedJob.company.logoUrl}
                  alt={selectedJob.company.name}
                  sx={{ width: 64, height: 64, objectFit: 'contain', borderRadius: 1 }}
                />
              ) : (
                <Box
                  sx={{
                    width: 64,
                    height: 64,
                    bgcolor: 'primary.light',
                    borderRadius: 1,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'primary.contrastText',
                    fontSize: 24,
                    fontWeight: 'bold',
                  }}
                >
                  {selectedJob.company.name.charAt(0).toUpperCase()}
                </Box>
              )}
              <Box sx={{ flex: 1 }}>
                <Typography variant="h5" gutterBottom>
                  {selectedJob.title}
                </Typography>
                <Typography variant="subtitle1" color="text.secondary">
                  {selectedJob.company.name}
                </Typography>
                {selectedJob.company.website && (
                  <Link
                    href={selectedJob.company.website}
                    target="_blank"
                    rel="noopener noreferrer"
                    sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5 }}
                  >
                    Besök hemsida <OpenInNewIcon fontSize="small" />
                  </Link>
                )}
              </Box>
            </Box>

            {/* Quick info */}
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 3 }}>
              {selectedJob.location && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <LocationIcon color="action" />
                  <Typography variant="body2">{selectedJob.location}</Typography>
                </Box>
              )}
              {selectedJob.workLocationType && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <BusinessIcon color="action" />
                  <Typography variant="body2">{workLocationLabels[selectedJob.workLocationType]}</Typography>
                </Box>
              )}
              {selectedJob.employmentType && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <WorkIcon color="action" />
                  <Typography variant="body2">{employmentTypeLabels[selectedJob.employmentType]}</Typography>
                </Box>
              )}
              {formatSalary() && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <MoneyIcon color="action" />
                  <Typography variant="body2" color="primary" fontWeight="medium">
                    {formatSalary()}
                  </Typography>
                </Box>
              )}
              {selectedJob.postedAt && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <ScheduleIcon color="action" />
                  <Typography variant="body2">Publicerad {formatDate(selectedJob.postedAt)}</Typography>
                </Box>
              )}
            </Box>

            {/* Tags */}
            {selectedJob.tags.length > 0 && (
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Kompetenser & taggar
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                  {selectedJob.tags.map((tag) => (
                    <SkillChip key={tag} skillName={tag} />
                  ))}
                </Box>
              </Box>
            )}

            <Divider sx={{ my: 2 }} />

            {/* Description */}
            {selectedJob.description && (
              <DescriptionBlock description={selectedJob.description} />
            )}

            {/* Required skills */}
            {selectedJob.requiredSkills && (
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Krav
                </Typography>
                <Typography variant="body2">{selectedJob.requiredSkills}</Typography>
              </Box>
            )}

            {/* Benefits */}
            {selectedJob.benefits && (
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Förmåner
                </Typography>
                <Typography variant="body2">{selectedJob.benefits}</Typography>
              </Box>
            )}

            {/* Experience */}
            {(selectedJob.experienceYearsMin || selectedJob.experienceYearsMax) && (
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Erfarenhet
                </Typography>
                <Typography variant="body2">
                  {selectedJob.experienceYearsMin && selectedJob.experienceYearsMax
                    ? `${selectedJob.experienceYearsMin} - ${selectedJob.experienceYearsMax} år`
                    : selectedJob.experienceYearsMin
                      ? `Minst ${selectedJob.experienceYearsMin} år`
                      : `Upp till ${selectedJob.experienceYearsMax} år`}
                </Typography>
              </Box>
            )}

            {/* Expires */}
            {selectedJob.expiresAt && (
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Sista ansökningsdag
                </Typography>
                <Typography variant="body2">{formatDate(selectedJob.expiresAt)}</Typography>
              </Box>
            )}

            {/* Saved job status */}
            {selectedJob.isSaved && (
              <Box sx={{ mt: 3 }}>
                <FormControl size="small" sx={{ minWidth: 200 }}>
                  <InputLabel>Status</InputLabel>
                  <Select
                    value={selectedJob.savedStatus || 'Saved'}
                    label="Status"
                    onChange={(e) => handleStatusChange(e.target.value as SavedJobStatus)}
                  >
                    {Object.entries(savedStatusLabels).map(([value, label]) => (
                      <MenuItem key={value} value={value}>
                        {label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Box>
            )}
          </Box>
        ) : (
          <Typography color="text.secondary">Kunde inte ladda jobbet.</Typography>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        {selectedJob && (
          <>
            <Button
              variant={selectedJob.isSaved ? 'outlined' : 'contained'}
              startIcon={selectedJob.isSaved ? <BookmarkIcon /> : <BookmarkBorderIcon />}
              onClick={handleSaveClick}
              disabled={isMutating}
            >
              {selectedJob.isSaved ? 'Sparad' : 'Spara jobb'}
            </Button>

            {(selectedJob.applicationUrl || selectedJob.externalUrl) && (
              <Button
                variant="contained"
                color="success"
                endIcon={<OpenInNewIcon />}
                component="a"
                href={selectedJob.applicationUrl || selectedJob.externalUrl || ''}
                target="_blank"
                rel="noopener noreferrer"
              >
                Ansök
              </Button>
            )}
          </>
        )}
      </DialogActions>
    </Dialog>
  );
}

const HTML_TAG_REGEX = /<\/?[a-z][\s\S]*?>/i;

function DescriptionBlock({ description }: { description: string }) {
  const isHtml = HTML_TAG_REGEX.test(description);

  const sanitizedHtml = useMemo(() => {
    if (!isHtml) return null;
    return DOMPurify.sanitize(description, {
      ALLOWED_TAGS: [
        'p', 'br', 'strong', 'b', 'em', 'i', 'u', 'ul', 'ol', 'li',
        'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'a', 'span', 'div',
        'table', 'thead', 'tbody', 'tr', 'th', 'td', 'blockquote', 'hr',
      ],
      ALLOWED_ATTR: ['href', 'target', 'rel'],
    });
  }, [description, isHtml]);

  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="subtitle2" gutterBottom>
        Beskrivning
      </Typography>
      {isHtml ? (
        <Box
          sx={{
            typography: 'body2',
            '& a': { color: 'primary.main' },
            '& ul, & ol': { pl: 3 },
            '& p': { mb: 1 },
          }}
          dangerouslySetInnerHTML={{ __html: sanitizedHtml! }}
        />
      ) : (
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
          {description}
        </Typography>
      )}
    </Box>
  );
}

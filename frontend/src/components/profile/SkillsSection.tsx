import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  TextField,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Grid,
  Card,
  CardContent,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Autocomplete,
  CircularProgress,
} from '@mui/material';
import { Delete as DeleteIcon, Add as AddIcon } from '@mui/icons-material';
import { useProfile, useAddSkill, useRemoveSkill } from '../../api/hooks/useProfile';
import { jobsApi } from '../../api/jobs';
import type { SkillLevel, AddSkillRequest } from '../../api/profile';

const skillLevelLabels: Record<SkillLevel, string> = {
  Beginner: 'Nybörjare',
  Intermediate: 'Mellannivå',
  Advanced: 'Avancerad',
  Expert: 'Expert',
};

const skillLevelColors: Record<SkillLevel, 'default' | 'primary' | 'secondary' | 'success'> = {
  Beginner: 'default',
  Intermediate: 'primary',
  Advanced: 'secondary',
  Expert: 'success',
};

export function SkillsSection() {
  const { data: profile } = useProfile();
  const addSkillMutation = useAddSkill();
  const removeSkillMutation = useRemoveSkill();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [newSkill, setNewSkill] = useState<AddSkillRequest>({
    name: '',
    level: 'Intermediate',
  });

  const [tagOptions, setTagOptions] = useState<string[]>([]);
  const [tagSearch, setTagSearch] = useState('');
  const [tagsLoading, setTagsLoading] = useState(false);

  const isLoading = addSkillMutation.isPending || removeSkillMutation.isPending;

  const fetchTags = useCallback(async (search: string) => {
    setTagsLoading(true);
    try {
      const tags = await jobsApi.getTags(search || undefined);
      setTagOptions(tags);
    } catch {
      setTagOptions([]);
    } finally {
      setTagsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!dialogOpen) return;

    const debounce = setTimeout(() => {
      fetchTags(tagSearch);
    }, 300);

    return () => clearTimeout(debounce);
  }, [tagSearch, dialogOpen, fetchTags]);

  const handleOpenDialog = () => {
    setNewSkill({ name: '', level: 'Intermediate' });
    setTagSearch('');
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
  };

  const handleAddSkill = async () => {
    if (newSkill.name.trim()) {
      await addSkillMutation.mutateAsync(newSkill);
      setDialogOpen(false);
    }
  };

  const handleRemoveSkill = (skillId: string) => {
    removeSkillMutation.mutate(skillId);
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Kompetenser</Typography>
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={handleOpenDialog}
          disabled={isLoading}
        >
          Lägg till kompetens
        </Button>
      </Box>

      {profile?.skills.length === 0 ? (
        <Typography color="text.secondary">
          Inga kompetenser tillagda ännu. Lägg till dina färdigheter för att matcha med relevanta jobb.
        </Typography>
      ) : (
        <Grid container spacing={2}>
          {profile?.skills.map((skill) => (
            <Grid size={{ xs: 12, sm: 6, md: 4 }} key={skill.id}>
              <Card variant="outlined">
                <CardContent sx={{ pb: 1, '&:last-child': { pb: 1 } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <Box>
                      <Typography variant="subtitle1" fontWeight="medium">
                        {skill.name}
                      </Typography>
                      <Box sx={{ mt: 0.5 }}>
                        <Chip
                          label={skillLevelLabels[skill.level]}
                          size="small"
                          color={skillLevelColors[skill.level]}
                        />
                      </Box>
                    </Box>
                    <IconButton
                      size="small"
                      onClick={() => handleRemoveSkill(skill.id)}
                      disabled={isLoading}
                      aria-label={`Ta bort ${skill.name}`}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Lägg till kompetens</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Autocomplete
              freeSolo
              options={tagOptions}
              loading={tagsLoading}
              inputValue={tagSearch}
              onInputChange={(_event, value) => {
                setTagSearch(value);
                setNewSkill((prev) => ({ ...prev, name: value }));
              }}
              onChange={(_event, value) => {
                if (typeof value === 'string') {
                  setNewSkill((prev) => ({ ...prev, name: value }));
                  setTagSearch(value);
                }
              }}
              disabled={isLoading}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Kompetens"
                  placeholder="Sök eller skriv en kompetens"
                  slotProps={{
                    input: {
                      ...params.InputProps,
                      endAdornment: (
                        <>
                          {tagsLoading ? <CircularProgress color="inherit" size={20} /> : null}
                          {params.InputProps.endAdornment}
                        </>
                      ),
                    },
                  }}
                />
              )}
            />

            <FormControl fullWidth>
              <InputLabel>Nivå</InputLabel>
              <Select
                value={newSkill.level}
                label="Nivå"
                onChange={(e) =>
                  setNewSkill((prev) => ({ ...prev, level: e.target.value as SkillLevel }))
                }
                disabled={isLoading}
              >
                {Object.entries(skillLevelLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Avbryt</Button>
          <Button
            variant="contained"
            onClick={handleAddSkill}
            disabled={isLoading || !newSkill.name.trim()}
          >
            Lägg till
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

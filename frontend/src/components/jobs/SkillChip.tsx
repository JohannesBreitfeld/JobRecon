import { useState } from 'react';
import { Chip, CircularProgress, Tooltip } from '@mui/material';
import {
  AddCircleOutline as AddCircleOutlineIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { useProfileStore } from '../../stores/profileStore';

interface SkillChipProps {
  skillName: string;
}

export function SkillChip({ skillName }: SkillChipProps) {
  const { profile, addSkill } = useProfileStore();
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState(false);

  const alreadyHave =
    profile?.skills.some((s) => s.name.toLowerCase() === skillName.toLowerCase()) ?? false;

  const handleAdd = async () => {
    setAdding(true);
    setError(false);
    try {
      await addSkill({ name: skillName, level: 'Intermediate' });
    } catch {
      setError(true);
      setTimeout(() => setError(false), 1500);
    } finally {
      setAdding(false);
    }
  };

  if (alreadyHave) {
    return (
      <Chip
        label={skillName}
        size="small"
        color="success"
        variant="outlined"
        icon={<CheckCircleIcon fontSize="small" />}
      />
    );
  }

  return (
    <Tooltip title={`Lägg till ${skillName} bland dina kompetenser`} disableHoverListener={adding}>
      <Chip
        label={skillName}
        size="small"
        variant="outlined"
        color={error ? 'error' : 'default'}
        onClick={adding ? undefined : handleAdd}
        onDelete={adding ? undefined : handleAdd}
        deleteIcon={
          adding ? (
            <CircularProgress size={14} sx={{ color: 'inherit' }} />
          ) : (
            <AddCircleOutlineIcon fontSize="small" />
          )
        }
        sx={{ cursor: adding ? 'default' : 'pointer' }}
      />
    </Tooltip>
  );
}

import { Chip, CircularProgress, Tooltip } from '@mui/material';
import {
  AddCircleOutline as AddCircleOutlineIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { useProfile, useAddSkill } from '../../api/hooks/useProfile';

interface SkillChipProps {
  skillName: string;
}

export function SkillChip({ skillName }: SkillChipProps) {
  const { data: profile } = useProfile();
  const addSkillMutation = useAddSkill();

  const alreadyHave =
    profile?.skills.some((s) => s.name.toLowerCase() === skillName.toLowerCase()) ?? false;

  const handleAdd = () => {
    addSkillMutation.mutate({ name: skillName, level: 'Intermediate' });
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

  const adding = addSkillMutation.isPending;
  const error = addSkillMutation.isError;

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

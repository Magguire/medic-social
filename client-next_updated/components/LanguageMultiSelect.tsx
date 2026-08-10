import Autocomplete from '@mui/material/Autocomplete';
import TextField from '@mui/material/TextField';

const commonLanguages = [
  'Arabic',
  'Bengali',
  'Cantonese',
  'Dutch',
  'English',
  'French',
  'German',
  'Greek',
  'Gujarati',
  'Hindi',
  'Italian',
  'Japanese',
  'Korean',
  'Mandarin',
  'Polish',
  'Portuguese',
  'Punjabi',
  'Russian',
  'Spanish',
  'Swahili',
  'Tagalog',
  'Tamil',
  'Thai',
  'Turkish',
  'Ukrainian',
  'Urdu',
  'Vietnamese',
  'Yoruba',
  'Zulu',
];

type Props = {
  label: string;
  value: string;
  onChange: (value: string) => void;
};

const splitLanguages = (value: string) =>
  value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);

export default function LanguageMultiSelect({ label, value, onChange }: Props) {
  const selected = splitLanguages(value);

  return (
    <Autocomplete
      multiple
      freeSolo
      options={commonLanguages}
      value={selected}
      onChange={(_, nextValue) => {
        const normalized = Array.from(new Set(nextValue.map((item) => String(item).trim()).filter(Boolean)));
        onChange(normalized.join(', '));
      }}
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          placeholder={selected.length === 0 ? 'Select or type languages' : ''}
          fullWidth
        />
      )}
      sx={{
        width: '100%',
        minWidth: 0,
        '& .MuiOutlinedInput-root': {
          borderRadius: '0.75rem',
          minHeight: '3rem',
          backgroundColor: 'var(--client-input)',
        },
      }}
    />
  );
}

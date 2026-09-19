export const colours = {
  navy: '#0B1F3A',
  navyMid: '#143056',
  amber: '#E3A008',
  paper: '#F3EFE6',
  card: '#FFFFFF',
  ink: '#1A2332',
  muted: '#5C6B7A',
  line: '#D7DEE6',
  green: '#1F7A4D',
  red: '#B42318',
  grey: '#8A94A0',
  white: '#FFFFFF',
};

export const trafficColour: Record<string, string> = {
  Green: colours.green,
  Amber: colours.amber,
  Red: colours.red,
  Grey: colours.grey,
};

export function trafficLabel(light: string): string {
  switch (light) {
    case 'Green':
      return 'In date';
    case 'Amber':
      return 'Due within 60 days';
    case 'Red':
      return 'Expired';
    default:
      return 'Not started';
  }
}

export function formatDate(value?: string | null): string {
  if (!value) {
    return 'No date';
  }
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) {
    return 'No date';
  }
  return new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }).format(d);
}

export function daysCopy(days?: number | null): string {
  if (days === null || days === undefined) {
    return '';
  }
  if (days < 0) {
    return `${Math.abs(days)} day${Math.abs(days) === 1 ? '' : 's'} overdue`;
  }
  if (days === 0) {
    return 'Expires today';
  }
  return `${days} day${days === 1 ? '' : 's'} left`;
}

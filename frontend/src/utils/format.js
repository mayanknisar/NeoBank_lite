export function maskAccountNumber(number) {
  if (!number || number.length < 4) return number;
  return `•••• ${number.slice(-4)}`;
}

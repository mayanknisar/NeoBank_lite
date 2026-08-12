const TONE_BY_STATUS = {
  ACTIVE: "good",
  VERIFIED: "good",
  PENDING: "warn",
  FROZEN: "bad",
  CLOSED: "bad",
  REJECTED: "bad"
};

export default function StatusStamp({ status }) {
  const tone = TONE_BY_STATUS[status] || "warn";
  return <span className={`stamp stamp--${tone}`}>{status}</span>;
}

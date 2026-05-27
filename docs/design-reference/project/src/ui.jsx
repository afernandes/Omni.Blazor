// UI primitives + icon set
const Icon = ({ name, size = 18, stroke = 1.75 }) => {
  const common = { width: size, height: size, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: stroke, strokeLinecap: 'round', strokeLinejoin: 'round' };
  switch (name) {
    case 'pos': return <svg {...common}><rect x="3" y="4" width="18" height="13" rx="2"/><path d="M7 20h10M10 17v3M14 17v3"/></svg>;
    case 'pizza': return <svg {...common}><path d="M12 2l10 10a14 14 0 0 1-20 0z"/><circle cx="10" cy="10" r="0.8" fill="currentColor" stroke="none"/><circle cx="14" cy="12" r="0.8" fill="currentColor" stroke="none"/><circle cx="11" cy="14" r="0.8" fill="currentColor" stroke="none"/></svg>;
    case 'menu': return <svg {...common}><rect x="5" y="2" width="14" height="20" rx="2"/><path d="M8 7h8M8 11h8M8 15h5"/></svg>;
    case 'hub': return <svg {...common}><rect x="3" y="4" width="5" height="16" rx="1"/><rect x="10" y="4" width="5" height="10" rx="1"/><rect x="17" y="4" width="4" height="13" rx="1"/></svg>;
    case 'waiter': return <svg {...common}><path d="M12 4a2 2 0 1 1 0 4 2 2 0 0 1 0-4z"/><path d="M4 14c0-3 3.5-4 8-4s8 1 8 4"/><path d="M3 18h18"/></svg>;
    case 'admin': return <svg {...common}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h.01a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09c0 .66.39 1.25 1 1.51a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v.01c.26.61.85 1 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>;
    case 'search': return <svg {...common}><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>;
    case 'user': return <svg {...common}><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 4-6 8-6s8 2 8 6"/></svg>;
    case 'user-plus': return <svg {...common}><circle cx="9" cy="8" r="4"/><path d="M2 21c0-4 3-6 7-6s7 2 7 6"/><path d="M20 8v6M17 11h6"/></svg>;
    case 'phone': return <svg {...common}><path d="M22 16.92v3a2 2 0 0 1-2.18 2A19.79 19.79 0 0 1 2.12 4.18 2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.1.74.27 1.47.5 2.17a2 2 0 0 1-.45 2.11L7.91 9.91a16 16 0 0 0 6 6l1.91-1.28a2 2 0 0 1 2.11-.45c.7.23 1.43.4 2.17.5a2 2 0 0 1 1.7 2.04z"/></svg>;
    case 'plus': return <svg {...common}><path d="M12 5v14M5 12h14"/></svg>;
    case 'minus': return <svg {...common}><path d="M5 12h14"/></svg>;
    case 'x': return <svg {...common}><path d="M18 6 6 18M6 6l12 12"/></svg>;
    case 'trash': return <svg {...common}><path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/></svg>;
    case 'chevron-right': return <svg {...common}><path d="M9 18l6-6-6-6"/></svg>;
    case 'chevron-left': return <svg {...common}><path d="M15 18l-9-6 9-6"/></svg>;
    case 'check': return <svg {...common}><path d="M20 6 9 17l-5-5"/></svg>;
    case 'cart': return <svg {...common}><circle cx="9" cy="20" r="1.5"/><circle cx="18" cy="20" r="1.5"/><path d="M2 3h3l2.5 13.5A2 2 0 0 0 9.5 18H18a2 2 0 0 0 2-1.5L22 7H6"/></svg>;
    case 'truck': return <svg {...common}><path d="M1 4h15v10H1zM16 8h5l2 3v3h-7M6 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM18 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4z"/></svg>;
    case 'bag': return <svg {...common}><path d="M5 6h14l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6z"/><path d="M9 10V6a3 3 0 0 1 6 0v4"/></svg>;
    case 'table': return <svg {...common}><rect x="3" y="4" width="18" height="6" rx="1"/><path d="M6 10v10M18 10v10"/></svg>;
    case 'store': return <svg {...common}><path d="M3 8l2-5h14l2 5v2a3 3 0 0 1-6 0 3 3 0 0 1-6 0 3 3 0 0 1-6 0z"/><path d="M4 10v10h16V10"/></svg>;
    case 'card': return <svg {...common}><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/></svg>;
    case 'cash': return <svg {...common}><rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/></svg>;
    case 'pix': return <svg {...common}><path d="M5 12l7-7 7 7-7 7z"/></svg>;
    case 'clock': return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>;
    case 'qrcode': return <svg {...common}><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><path d="M14 14h3v3h-3zM20 14v7M14 20h7"/></svg>;
    case 'star': return <svg {...common}><path d="M12 2l3 7 7 1-5 5 1 7-6-3-6 3 1-7-5-5 7-1z"/></svg>;
    case 'bell': return <svg {...common}><path d="M6 8a6 6 0 1 1 12 0c0 7 3 8 3 8H3s3-1 3-8M10 21a2 2 0 0 0 4 0"/></svg>;
    case 'print': return <svg {...common}><path d="M6 9V2h12v7M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2M6 14h12v8H6z"/></svg>;
    case 'flame': return <svg {...common}><path d="M12 2s4 4 4 8a4 4 0 0 1-8 0c0-1 .5-2 .5-2S6 11 6 14a6 6 0 0 0 12 0c0-6-6-12-6-12z"/></svg>;
    case 'sparkle': return <svg {...common}><path d="M12 3l2 5 5 2-5 2-2 5-2-5-5-2 5-2z"/></svg>;
    case 'sliders': return <svg {...common}><path d="M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6"/></svg>;
    case 'moon': return <svg {...common}><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>;
    case 'sun': return <svg {...common}><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>;
    case 'wifi': return <svg {...common}><path d="M5 12.55a11 11 0 0 1 14 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>;
    case 'battery': return <svg {...common}><rect x="2" y="7" width="18" height="10" rx="2"/><path d="M22 10v4"/></svg>;
    default: return null;
  }
};

window.Icon = Icon;

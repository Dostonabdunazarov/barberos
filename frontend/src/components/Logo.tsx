import { useId } from "react";

/**
 * Винтажный знак-картуш BarberKing — декоративная золотая рамка с завитками,
 * BARBERSHOP в центре, BARBERKING по верхней дуге и HAIRCUT AND SHAVES по нижней.
 * Оригинальная векторная эмблема в стиле классического барбершоп-бейджа
 * (не копия чужого лого). Для хедера/футера. Виден на тёмной сцене.
 */
export function LogoMark({ className }: { className?: string }) {
  const id = useId();
  const g = `url(#${id})`;
  const gold = "#d4a95f";
  return (
    <svg
      viewBox="0 0 300 200"
      className={className}
      fill="none"
      role="img"
      aria-label="BarberKing"
    >
      <defs>
        <linearGradient id={id} gradientUnits="userSpaceOnUse" x1="150" y1="24" x2="150" y2="176">
          <stop offset="0" stopColor="#f0d9a8" />
          <stop offset="0.5" stopColor="#e6c584" />
          <stop offset="1" stopColor="#c49a52" />
        </linearGradient>
        <path id={`${id}-top`} d="M 60 96 A 92 92 0 0 1 240 96" fill="none" />
        <path id={`${id}-bot`} d="M 66 104 A 88 88 0 0 0 234 104" fill="none" />
      </defs>

      {/* Верхняя дуга: BARBERKING */}
      <text
        fontFamily="Inter, Arial, sans-serif"
        fontSize="15"
        fontWeight="700"
        letterSpacing="3.5"
        fill="#efe6d2"
      >
        <textPath href={`#${id}-top`} startOffset="50%" textAnchor="middle">
          BARBERKING
        </textPath>
      </text>

      {/* Центральная лента-картуш */}
      <g stroke={g} fill="none" strokeWidth="2.2" strokeLinecap="round">
        <path d="M40 100 H260" />
        <path d="M40 100 q-8 0 -8 8 M260 100 q8 0 8 8" />
        <path d="M40 100 q-8 0 -8 -8 M260 100 q8 0 8 -8" />
        {/* завитки по краям ленты */}
        <path d="M52 88 q14 -10 30 -6 q-10 -8 -24 -4" />
        <path d="M248 88 q-14 -10 -30 -6 q10 -8 24 -4" />
        <path d="M52 112 q14 10 30 6 q-10 8 -24 4" />
        <path d="M248 112 q-14 10 -30 6 q10 8 24 4" />
        {/* центральная лилия-флёрон сверху и снизу */}
        <path d="M150 78 q6 -12 0 -22 q-6 10 0 22 M150 78 q-8 -8 -16 -6 M150 78 q8 -8 16 -6" />
        <path d="M150 122 q6 12 0 22 q-6 -10 0 -22 M150 122 q-8 8 -16 6 M150 122 q8 8 16 6" />
      </g>

      {/* BARBERSHOP в центре */}
      <text
        x="150"
        y="108"
        fontFamily="'Cormorant Garamond', Georgia, serif"
        fontSize="34"
        fontWeight="700"
        letterSpacing="1"
        textAnchor="middle"
        fill="#f6efdd"
      >
        BARBERSHOP
      </text>

      {/* Нижняя дуга: HAIRCUT AND SHAVES */}
      <text
        fontFamily="Inter, Arial, sans-serif"
        fontSize="13"
        fontWeight="600"
        letterSpacing="3"
        fill={gold}
      >
        <textPath href={`#${id}-bot`} startOffset="50%" textAnchor="middle">
          HAIRCUT AND SHAVES
        </textPath>
      </text>
    </svg>
  );
}

/**
 * Полный текстовый лок-ап: HAIR STYLE · BARBERKING (дугой) · ножницы · SINCE.
 * Для витринных мест (Hero, страница входа).
 */
export function LogoLockup({ className }: { className?: string }) {
  const id = useId();
  const g = `url(#${id})`;
  return (
    <svg viewBox="0 0 440 250" className={className} fill="none" overflow="visible" role="img" aria-label="BarberKing">
      <defs>
        <linearGradient id={id} gradientUnits="userSpaceOnUse" x1="220" y1="20" x2="220" y2="230">
          <stop offset="0" stopColor="#f0d9a8" />
          <stop offset="0.5" stopColor="#e6c584" />
          <stop offset="1" stopColor="#c49a52" />
        </linearGradient>
      </defs>

      {/* HAIR STYLE + боковые линии */}
      <text
        x="220"
        y="48"
        fontFamily="Inter, Arial, sans-serif"
        fontSize="18"
        fontWeight="700"
        letterSpacing="6"
        textAnchor="middle"
        fill="#d4a95f"
      >
        HAIR STYLE
      </text>
      <line x1="95" y1="42" x2="145" y2="42" stroke="#8f6528" strokeWidth="1.5" />
      <line x1="295" y1="42" x2="345" y2="42" stroke="#8f6528" strokeWidth="1.5" />

      {/* BARBERKING — прямым текстом (без дуги): textPath в iOS Safari обрезал
          финальную «G». Прямой центрированный текст рендерится надёжно везде. */}
      <text
        x="220"
        y="150"
        fontFamily="'Cormorant Garamond', Georgia, serif"
        fontSize="52"
        fontWeight="700"
        letterSpacing="1"
        textAnchor="middle"
        fill={g}
      >
        BARBERKING
      </text>

      {/* Скрещённые ножницы */}
      <g stroke={g} fill="none" strokeLinecap="round" transform="translate(180,162) scale(0.68)">
        <circle cx="30" cy="54" r="7" strokeWidth="3.5" />
        <path d="M35 49 L 90 12" strokeWidth="4.5" />
        <circle cx="90" cy="54" r="7" strokeWidth="3.5" />
        <path d="M85 49 L 30 12" strokeWidth="4.5" />
      </g>

      {/* SINCE + боковые линии */}
      <text
        x="220"
        y="238"
        fontFamily="Inter, Arial, sans-serif"
        fontSize="14"
        fontWeight="600"
        letterSpacing="7"
        textAnchor="middle"
        fill="#a8a097"
      >
        SINCE 2026
      </text>
      <line x1="110" y1="233" x2="148" y2="233" stroke="#8f6528" strokeWidth="1" />
      <line x1="292" y1="233" x2="330" y2="233" stroke="#8f6528" strokeWidth="1" />
    </svg>
  );
}

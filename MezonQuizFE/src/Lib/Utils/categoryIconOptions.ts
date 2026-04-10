import {
  MdBiotech,
  MdCalculate,
  MdCode,
  MdEmojiEvents,
  MdHistoryEdu,
  MdLanguage,
  MdMap,
  MdMenuBook,
  MdMovie,
  MdMusicNote,
  MdPalette,
  MdPsychology,
  MdSportsEsports,
} from "react-icons/md";
import type { IconType } from "react-icons";

export interface CategoryIconOption {
  key: string;
  label: string;
  color: string;
  background: string;
  icon: IconType;
}

export const CATEGORY_ICON_OPTIONS: CategoryIconOption[] = [
  { key: "book", label: "Book", color: "#1d4ed8", background: "#dbeafe", icon: MdMenuBook },
  { key: "brain", label: "Brain", color: "#7c3aed", background: "#ede9fe", icon: MdPsychology },
  { key: "code", label: "Code", color: "#0f766e", background: "#ccfbf1", icon: MdCode },
  { key: "math", label: "Math", color: "#be123c", background: "#ffe4e6", icon: MdCalculate },
  { key: "sci", label: "Science", color: "#047857", background: "#d1fae5", icon: MdBiotech },
  { key: "hist", label: "History", color: "#92400e", background: "#fef3c7", icon: MdHistoryEdu },
  { key: "geo", label: "Geography", color: "#0369a1", background: "#e0f2fe", icon: MdMap },
  { key: "sport", label: "Sport", color: "#166534", background: "#dcfce7", icon: MdSportsEsports },
  { key: "music", label: "Music", color: "#9d174d", background: "#fce7f3", icon: MdMusicNote },
  { key: "art", label: "Art", color: "#c2410c", background: "#ffedd5", icon: MdPalette },
  { key: "lang", label: "Language", color: "#1e40af", background: "#dbeafe", icon: MdLanguage },
  { key: "exam", label: "Exam", color: "#0f766e", background: "#ccfbf1", icon: MdEmojiEvents },
  { key: "movie", label: "Movie", color: "#7860a1", background: "#ede9fe", icon: MdMovie },
];

export const getCategoryIconOption = (key?: string | null): CategoryIconOption | null => {
  if (!key) {
    return null;
  }

  return CATEGORY_ICON_OPTIONS.find((option) => option.key === key) ?? null;
};

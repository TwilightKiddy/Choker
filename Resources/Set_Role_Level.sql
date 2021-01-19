INSERT INTO roles (id, level)
  VALUES($id, $level)
  ON CONFLICT(id)
  DO UPDATE SET level=excluded.level;
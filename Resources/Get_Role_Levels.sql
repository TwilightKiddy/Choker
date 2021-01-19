SELECT id, level
  FROM roles
  WHERE id in ($ids);
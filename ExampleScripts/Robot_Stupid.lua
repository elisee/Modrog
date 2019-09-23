function tick(self)
  local random = math.random(1, 8)

  if random == 1 then
    self:rotateCW()
  elseif random == 2 then
    self:rotateCCW()
  else
    self:move()
  end
end

-- Workloads of the benchmark. Keep the behavior identical to guest/src/lib.rs:
-- the harness compares the checksums of both implementations.
--
-- The script does not use integer division or bitwise operators so that it also runs on LuaJIT.
-- Functions that are called in loops are cached in locals, which is the idiomatic way to make them fast.

local host = CS.WaaS.Benchmarks.LuaBenchmarkHost.Instance
local Vector3 = CS.UnityEngine.Vector3

local MODULUS = 1000000007
local sort = table.sort
local byte, upper, format, gmatch = string.byte, string.upper, string.format, string.gmatch

local M = {}

local function fib(n)
    if n < 2 then
        return n
    end
    return fib(n - 1) + fib(n - 2)
end

function M.compute_fib(n)
    return fib(n)
end

function M.compute_sieve(n)
    local composite = {}
    for i = 1, n do
        composite[i] = false
    end
    local count = 0
    for i = 2, n do
        if not composite[i] then
            count = count + 1
            for j = i * i, n, i do
                composite[j] = true
            end
        end
    end
    return count
end

function M.compute_mandelbrot(n)
    local total = 0
    for y = 0, n - 1 do
        local ci = 2.0 * y / n - 1.0
        for x = 0, n - 1 do
            local cr = 2.0 * x / n - 1.5
            local zr, zi = 0.0, 0.0
            local i = 0
            while i < 50 and zr * zr + zi * zi <= 4.0 do
                zr, zi = zr * zr - zi * zi + cr, 2.0 * zr * zi + ci
                i = i + 1
            end
            total = total + i
        end
    end
    return total
end

function M.compute_strings(n)
    local total = 0
    for i = 0, n - 1 do
        local s = format("item-%d-%d", i, i * 7919 % 65536)
        local u = upper(s)
        for word in gmatch(u, "[^-]+") do
            total = total + #word
        end
        for k = 1, #u do
            total = (total * 31 + byte(u, k)) % MODULUS
        end
    end
    return total
end

function M.compute_sort(n)
    local x = 42
    local values = {}
    for i = 1, n do
        x = x * 16807 % 2147483647
        values[i] = x
    end
    sort(values)
    local total = 0
    for i = 1, n do
        total = (total + values[i] % 65536 * i) % MODULUS
    end
    return total
end

function M.compute_map(n)
    local x = 42
    local counts = {}
    for _ = 1, n do
        x = x * 16807 % 2147483647
        local key = x % 4096
        counts[key] = (counts[key] or 0) + 1
    end
    local total = 0
    for key = 0, 4095 do
        local count = counts[key]
        if count then
            total = (total * 31 + key * count) % MODULUS
        end
    end
    return total
end

function M.ffi_nop(n)
    local Nop = host.Nop
    for _ = 1, n do
        Nop(host)
    end
    return n
end

function M.ffi_add(n)
    local Add = host.Add
    local value = 0
    for _ = 1, n do
        value = Add(host, value, 1)
    end
    return value
end

function M.ffi_string_in(n)
    local TakeString = host.TakeString
    local total = 0
    for _ = 1, n do
        total = total + TakeString(host, "hello, world")
    end
    return total
end

function M.ffi_string_out(n)
    local GetString = host.GetString
    local total = 0
    for _ = 1, n do
        total = total + #GetString(host)
    end
    return total
end

function M.ffi_struct(n)
    local Transform = host.Transform
    local value = Vector3(0, 0, 0)
    for _ = 1, n do
        value = Transform(host, value)
    end
    return value.x + value.y + value.z
end

function M.ffi_resource(n)
    local counter = host:GetCounter()
    local Add = counter.Add
    local value = 0
    for _ = 1, n do
        value = Add(counter, 1)
    end
    return value
end

function M.callback_add(a, b)
    return a + b
end

function M.callback_string(value)
    return #value
end

return M

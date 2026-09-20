<script setup lang="ts">
import Navbar from './components/Navbar.vue'
</script>

<template>
  <div class="min-h-screen bg-bg">
    <Navbar />
    <router-view />
  </div>
</template>